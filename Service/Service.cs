﻿//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//********************************************************************************************

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Generic;
using Cliver;
using System.Configuration;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Input;
using System.Net.Http;
using Zeroconf;
using System.Diagnostics;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.ServiceProcess;
using System.ServiceModel;

namespace Cliver.CisteraScreenCaptureService
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();

            CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Log.Main.Inform("Starting...");
                UiApi.OpenApi();

                //try
                //{
                //    uint dwSessionId = WinApi.Wts.WTSGetActiveConsoleSessionId();
                //    MpegStream.Start(dwSessionId, "-f gdigrab -framerate 10 -f rtp_mpegts -srtp_out_suite AES_CM_128_HMAC_SHA1_80 -srtp_out_params aMg7BqN047lFN72szkezmPyN1qSMilYCXbqP/sCt srtp://127.0.0.1:5920");
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine(e);
                //}

                uint sessionId = WinApi.Wts.WTSGetActiveConsoleSessionId();
                if (sessionId == 0 || sessionId == 0xFFFFFFFF)
                    Log.Main.Inform("No console user is active.");
                else
                    sessionChanged(sessionId, true);
            }
            catch(Exception e)
            {
                Log.Main.Error(e);
                throw e;
            }
        }

        protected override void OnStop()
        {
            try
            {
                Log.Main.Inform("Stopping...");
                stopServingUser();
                UiApi.CloseApi();
            }
            catch (Exception e)
            {
                Log.Main.Error(e);
                throw e;
            }
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            Log.Main.Write("Session: " + changeDescription.SessionId + " : " + changeDescription.Reason);
            switch (changeDescription.Reason)
            {
                case SessionChangeReason.ConsoleConnect:
                case SessionChangeReason.RemoteConnect:
                case SessionChangeReason.SessionUnlock:
                    sessionChanged((uint)changeDescription.SessionId, true);
                    break;
                default:
                    sessionChanged((uint)changeDescription.SessionId, false);
                    break;
            }

            base.OnSessionChange(changeDescription);
        }

        internal static void sessionChanged(uint sessionId, bool active)
        {
            try
            {
                if (sessionId == 0 || !active)
                {
                    Log.Main.Inform("User logged off: " + currentUserName);
                    stopServingUser();
                    currentUserSessionId = 0;
                    currentUserName = null;
                    return;
                }

                string userName = WindowsUserRoutines.GetUserNameBySessionId(sessionId);
                if (userName == currentUserName)
                    return;

                Log.Main.Inform("User logged in: " + userName);
                stopServingUser();
                currentUserSessionId = sessionId;
                currentUserName = userName;
                if (string.IsNullOrWhiteSpace(currentUserName))
                {
                    Log.Main.Error("Session's user name is empty.");
                    return;
                }
                onNewUser_t = ThreadRoutines.StartTry(
                    () =>
                    {
                        string service = Settings.General.GetServiceName();
                        IReadOnlyList<IZeroconfHost> zhs = ZeroconfResolver.ResolveAsync(service, TimeSpan.FromSeconds(3), 1, 10).Result;
                        if (zhs.Count < 1)
                        {
                            currentServerIp = Settings.General.TcpClientDefaultIp;
                            string m = "Service '" + service + "' could not be resolved.\r\nUsing default ip: " + currentServerIp;
                            Log.Main.Warning(m);
                            UiApi.Message(MessageType.WARNING, m);
                        }
                        else if (zhs.Where(x => x.IPAddress != null).FirstOrDefault() == null)
                        {
                            currentServerIp = Settings.General.TcpClientDefaultIp;
                            string m = "Resolution of service '" + service + "' has no IP defined.\r\nUsing default ip: " + currentServerIp;
                            Log.Main.Error(m);
                            UiApi.Message(MessageType.ERROR, m);
                        }
                        else
                        {
                            currentServerIp = zhs.Where(x => x.IPAddress != null).FirstOrDefault().IPAddress;
                            Log.Main.Inform("Service: " + service + " has been resolved to: " + currentServerIp);
                        }

                        IPAddress ip;
                        if (!IPAddress.TryParse(currentServerIp, out ip))
                            throw new Exception("Server IP is not valid: " + currentServerIp);
                        TcpServer.Start(Settings.General.TcpServerPort, ip);

                        string url = "http://" + currentServerIp + "/screenCapture/register?username=" + currentUserName + "&ipaddress=" + TcpServer.LocalIp + "&port=" + TcpServer.LocalPort;
                        Log.Main.Inform("GETting: " + url);

                        HttpClient hc = new HttpClient();
                        HttpResponseMessage rm = hc.GetAsync(url).Result;
                        if (!rm.IsSuccessStatusCode)
                            throw new Exception(rm.ReasonPhrase);
                        if (rm.Content == null)
                            throw new Exception("Response is empty");
                        string responseContent = rm.Content.ReadAsStringAsync().Result;
                        if (responseContent.Trim().ToUpper() != "OK")
                            throw new Exception("Response body: " + responseContent);
                        Log.Main.Inform("Response body: " + responseContent);
                    },
                    (Exception e) =>
                    {
                        Log.Main.Error(e);
                        TcpServer.Stop();
                        UiApi.Message(MessageType.ERROR, Log.GetExceptionMessage(e));
                    },
                    () =>
                    {
                        onNewUser_t = null;
                    }
                );
            }
            catch(Exception e)
            {
                Log.Main.Error(e);
            }
        }
        static Thread onNewUser_t = null;
        static string currentUserName;
        public static string UserName
        {
            get
            {
                return currentUserName;
            }
        }
        static uint currentUserSessionId;
        public static uint UserSessionId
        {
            get
            {
                return currentUserSessionId;
            }
        }
        static string currentServerIp;
        public static string ServerIp
        {
            get
            {
                return currentServerIp;
            }
        }

        static void stopServingUser()
        {
            if (onNewUser_t != null)
            {
                while (onNewUser_t.IsAlive)
                {
                    Log.Main.Write("Terminating onNewUser_t...");
                    onNewUser_t.Abort();
                    onNewUser_t.Join(200);
                }
                onNewUser_t = null;
            }
            TcpServer.Stop();
            MpegStream.Stop();
        }
    }
}

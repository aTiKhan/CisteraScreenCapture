//********************************************************************************************
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
using System.Net.Mail;
using Cliver;
using System.Configuration;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.ServiceModel;

namespace Cliver.CisteraScreenCaptureService
{
    [ServiceContract(Namespace = "", SessionMode = SessionMode.Required, CallbackContract = typeof(IUiApiCallback))]
    public interface IUiApi
    {
        [OperationContract(IsOneWay = true, IsInitiating = true)]
        void Subscribe();

        [OperationContract(IsOneWay = true)]
        void Unsubscribe();

        [OperationContract()]
        Settings.GeneralSettings GetSettings();

        [OperationContract()]
        void SaveSettings(Settings.GeneralSettings general);

        //[OperationContract()]
        //string GetSettingsFile();
        
        [OperationContract(IsInitiating = true)]
        string GetLogDir();
    }

    public interface IUiApiCallback
    {
        [OperationContract(IsOneWay = true)]
        void Message(MessageType messageType, string message);
    }
    public enum MessageType
    {
        INFORM,
        WARNING,
        ERROR,
    }

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
    public class UiApi : IUiApi
    {
        public void Subscribe()
        {
            subscribe();
        }
        static readonly HashSet<IUiApiCallback> uiApiCallbacks = new HashSet<IUiApiCallback>();

        static void subscribe()
        {
            lock (uiApiCallbacks)
            {
                IUiApiCallback uiApiCallback = OperationContext.Current.GetCallbackChannel<IUiApiCallback>();
                if (uiApiCallbacks.Contains(uiApiCallback))
                    return;
                uiApiCallbacks.Add(uiApiCallback);                
                Log.Main.Write("Subscribed: " + uiApiCallbacks.Count + "\r\n" + Log.GetStackString(0, 3));
            }
        }

        public void Unsubscribe()
        {
            lock (uiApiCallbacks)
            {
                IUiApiCallback uiApiCallback = OperationContext.Current.GetCallbackChannel<IUiApiCallback>();
                uiApiCallbacks.Remove(uiApiCallback);
                Log.Main.Write("Subscribed2: " + uiApiCallbacks.Count);
            }
        }

        public Settings.GeneralSettings GetSettings()
        {
            subscribe();
            return Settings.General.GetReloadedInstance<Settings.GeneralSettings>();
        }

        public void SaveSettings(Settings.GeneralSettings general)
        {
            subscribe();
            general.Save(Settings.General.__File);
        }

        //public string GetSettingsFile()
        //{
        //    return Settings.General.__File;
        //}

        public string GetLogDir()
        {
            return Log.WorkDir;
        }        

        UiApi()
        {
        }

        static internal void OpenApi()
        {
            if (serviceHost == null)
            {
                lock (uiApiCallbacks)
                {
                    if (serviceHost != null)
                        return;
                    Log.Main.Inform("Opening UI API.");
                    uiApiCallbacks.Clear();

                    //NetNamedPipeBinding binding = new NetNamedPipeBinding(); binding.SendTimeout = TimeSpan.MaxValue; binding.ReceiveTimeout = TimeSpan.MaxValue;
                    serviceHost = new ServiceHost(typeof(UiApi));
                    serviceHost.Open();
                }
            }
        }
        static ServiceHost serviceHost = null;

        static internal void CloseApi()
        {
            lock (uiApiCallbacks)
            {
                if (serviceHost != null)
                {
                    Log.Main.Inform("Closing UI API.");
                    uiApiCallbacks.Clear();
                    serviceHost.Close();
                    serviceHost = null;
                }
            }
        }

        static void doCallback(Action<IUiApiCallback> mi)
        {
            ThreadRoutines.StartTry(() =>
            {
                lock (uiApiCallbacks)
                {
                    List<IUiApiCallback> dead_uacs = new List<IUiApiCallback>();
                    foreach (IUiApiCallback uiApiCallback in uiApiCallbacks)
                    {
                        try
                        {
                            mi(uiApiCallback);
                        }
                        catch (System.ServiceModel.CommunicationObjectAbortedException e)
                        {
                            //Log.Main.Warning2(e);
                            dead_uacs.Add(uiApiCallback);
                        }
                    }
                    foreach (IUiApiCallback dead_uac in dead_uacs)
                        uiApiCallbacks.Remove(dead_uac);
                }
            });
        }

        internal static void Message(MessageType messageType, string message)
        {
            doCallback((IUiApiCallback uiApiCallback) =>
            {
                uiApiCallback.Message(messageType, message);
            });
        }
    }
}
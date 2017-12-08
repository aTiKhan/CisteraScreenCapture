//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//        27 February 2007
//Copyright: (C) 2007, Sergey Stoyan
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
using System.ServiceProcess;
using Cliver.CisteraScreenCaptureService;
using System.Runtime.InteropServices;

/// <summary>
/// TBD:
/// - add display area;
/// </summary>
/// 
namespace Cliver.CisteraScreenCaptureUI
{
    [CallbackBehaviorAttribute(UseSynchronizationContext = false)]
    public class UiApiCallback : CisteraScreenCaptureService.IUiApiCallback
    {
        public void ServiceStatusChanged(System.ServiceProcess.ServiceControllerStatus status)
        {
            SysTray.This.ServiceStateChanged(status);
        }

        public void Message(MessageType messageType, string message)
        {
            if (!Settings.View.DisplayNotifications)
                return;
            switch (messageType)
            {
                case MessageType.INFORM:
                    InfoWindow.Create(message, null, "OK", null);
                    break;
                case MessageType.WARNING:
                    InfoWindow.Create(message, null, "OK", null, Settings.View.ErrorSoundFile, System.Windows.Media.Brushes.WhiteSmoke, System.Windows.Media.Brushes.Yellow);
                    break;
                case MessageType.ERROR:
                    InfoWindow.Create(message, null, "OK", null, Settings.View.ErrorSoundFile, System.Windows.Media.Brushes.WhiteSmoke, System.Windows.Media.Brushes.Red);
                    break;
                default:
                    throw new Exception("Unknown option: " + messageType);
            }
        }
    }

    public partial class UiApiClient
    {
        static UiApiClient()
        {
            try
            {
                //IntPtr hSCM = WinApi.Advapi32.OpenSCManager(null, null, WinApi.Advapi32.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
                //if (hSCM != IntPtr.Zero)
                //{
                //    IntPtr hService = WinApi.Advapi32.OpenService(hSCM, SERVICE_NAME, (WinApi.Advapi32.SCM_ACCESS)0xF003F);// WinApi.Advapi32.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
                //    if (hService != IntPtr.Zero)
                //    {
                //        notify = new WinApi.Advapi32.SERVICE_NOTIFY();
                //        notify.dwVersion = 2;
                //        notify.pfnNotifyCallback = Marshal.GetFunctionPointerForDelegate(changeDelegate);
                //        notify.pContext = IntPtr.Zero;
                //        notify.dwNotificationStatus = 0;
                //        WinApi.Advapi32.SERVICE_STATUS_PROCESS process;
                //        process.dwServiceType = 0;
                //        process.dwCurrentState = 0;
                //        process.dwControlsAccepted = 0;
                //        process.dwWin32ExitCode = 0;
                //        process.dwServiceSpecificExitCode = 0;
                //        process.dwCheckPoint = 0;
                //        process.dwWaitHint = 0;
                //        process.dwProcessId = 0;
                //        process.dwServiceFlags = 0;
                //        notify.ServiceStatus = process;
                //        notify.dwNotificationTriggered = 0;
                //        notify.pszServiceNames = Marshal.StringToHGlobalUni(SERVICE_NAME);
                //        notifyHandle = GCHandle.Alloc(notify, GCHandleType.Pinned);
                //        unmanagedNotifyStructure = notifyHandle.AddrOfPinnedObject();
                //        if (0 != WinApi.Advapi32.NotifyServiceStatusChange(hService, WinApi.Advapi32.NotifyMask.SERVICE_NOTIFY_START_PENDING | WinApi.Advapi32.NotifyMask.SERVICE_NOTIFY_RUNNING | WinApi.Advapi32.NotifyMask.SERVICE_NOTIFY_STOPPED, unmanagedNotifyStructure))
                //            LogMessage.Error(ErrorRoutines.GetLastError());
                //    }
                //}




                InstanceContext context = new InstanceContext(new UiApiCallback());
                This = new CisteraScreenCaptureService.UiApiClient(context);
                This.Subscribe();
            }
            catch (Exception e)
            {
                LogMessage.Error(e);
            }
        }
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void StatusChanged(IntPtr parameter);
        public static void ReceivedStatusChangedEvent(IntPtr parameter)
        {
            LogMessage.Write("status");
        }
        readonly public static StatusChanged changeDelegate = ReceivedStatusChangedEvent;
        public static WinApi.Advapi32.SERVICE_NOTIFY notify;
        public static GCHandle notifyHandle;
        public static IntPtr unmanagedNotifyStructure;




        readonly public static CisteraScreenCaptureService.UiApiClient This = null;

        static public void StartStop(bool start)
        {
            try
            {
                double timeoutSecs = 20;
                ServiceController serviceController = new ServiceController(SERVICE_NAME);
                if (start)
                {
                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeoutSecs));
                    if (serviceController.Status != ServiceControllerStatus.Running)
                        Message.Error("Could not start service '" + SERVICE_NAME + "' within " + timeoutSecs + " secs.");
                }
                else
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSecs));
                    if (serviceController.Status != ServiceControllerStatus.Stopped)
                        Message.Error("Could not stop service '" + SERVICE_NAME + "' within " + timeoutSecs + " secs.");
                }
            }
            catch (Exception ex)
            {
                LogMessage.Error(ex);
            }
            finally
            {
            }
        }

        static public ServiceControllerStatus? GetStatus()
        {
            try
            {
                ServiceController serviceController = new ServiceController(SERVICE_NAME);
                return serviceController.Status;
            }
            catch (Exception e)
            {
                LogMessage.Error(e);
            }
            return null;
        }
        public const string SERVICE_NAME = "Cistera Screen Capture Service";
    }
}
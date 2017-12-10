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
    [CallbackBehaviorAttribute(UseSynchronizationContext = true)]
    public class UiApiCallback : CisteraScreenCaptureService.IUiApiCallback
    {
        public void ServiceStatusChanged(System.ServiceProcess.ServiceControllerStatus status)
        {
            try
            {
                SysTray.This.ServiceStateChanged(status);
                UiApiClient.BeginWatchingService();
            }
            catch (Exception e)
            {
                Log.Main.Error(e);
            }
        }

        public void Message(MessageType messageType, string message)
        {
            try
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
            catch (Exception e)
            {
                Log.Main.Error(e);
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

                instanceContext = new InstanceContext(new UiApiCallback());
                BeginWatchingService();
            }
            catch (Exception e)
            {
                LogMessage.Error(e);
            }
        }
        //[UnmanagedFunctionPointer(CallingConvention.StdCall)]
        //public delegate void StatusChanged(IntPtr parameter);
        //public static void ReceivedStatusChangedEvent(IntPtr parameter)
        //{
        //    LogMessage.Write("status");
        //}
        //readonly public static StatusChanged changeDelegate = ReceivedStatusChangedEvent;
        //public static WinApi.Advapi32.SERVICE_NOTIFY notify;
        //public static GCHandle notifyHandle;
        //public static IntPtr unmanagedNotifyStructure;
        readonly static InstanceContext instanceContext;
        static CisteraScreenCaptureService.UiApiClient _this;

        static internal void BeginWatchingService()
        {
            lock (instanceContext)
            {
                //if (watch_service_t != null && watch_service_t.IsAlive)
                //    return;
                if (watch_service_t != null && watch_service_t.IsAlive)
                    watch_service_t.Abort();
                watch_service_t = ThreadRoutines.StartTry(
                    () =>
                    {
                        _this = new CisteraScreenCaptureService.UiApiClient(instanceContext);
                        for (; ; )
                        {
                            subscribe();
                            keep_alive_connection();//it seems to be redundant because of infinite timeout, but sometimes the channel gets closed due to errors
                        }
                    },
                    (Exception e) => 
                    {
                        LogMessage.Error(e);
                    },
                    null
                    );
            }
        }
        static Thread watch_service_t = null;
        static void subscribe()
        {
            bool contextUpdated = false;
            do
            {
                try
                {
                    lock (instanceContext)
                    {
                        _this?.Subscribe();
                    }
                }
                catch (Exception ex)
                {
                    if (!contextUpdated)
                        contextUpdated = true;
                    else
                    {
                        SysTray.This.ServiceStateChanged(ServiceControllerStatus.Stopped);
                        Thread.Sleep(Settings.View.ServiceStartPollTimeInMss);
                    }
                    _this = new CisteraScreenCaptureService.UiApiClient(instanceContext);                    
                }
                if (Thread.CurrentThread != watch_service_t)
                    return;
            }
            while (_this.State != CommunicationState.Opened);
            SysTray.This.ServiceStateChanged(ServiceControllerStatus.Running);
        }
        static void keep_alive_connection()
        {
            if (Thread.CurrentThread != watch_service_t)
                return;
            do
            {
                try
                {
                    lock (instanceContext)
                    {
                        _this.Subscribe();
                        SysTray.This.ServiceStateChanged(ServiceControllerStatus.Running);
                    }
                    Thread.Sleep(Settings.View.ServiceConnectionKeepAlivePulseTimeInMss);
                }
                catch (Exception ex)
                {
                }
                if (Thread.CurrentThread != watch_service_t)
                    return;
            }
            while (_this.State == CommunicationState.Opened);
            SysTray.This.ServiceStateChanged(ServiceControllerStatus.Stopped);
        }

        static void trySubscribeNewInstance()
        {
            try
            {
                lock (instanceContext)
                {
                    _this = new CisteraScreenCaptureService.UiApiClient(instanceContext);
                    _this?.Subscribe();
                }
                SysTray.This.ServiceStateChanged(ServiceControllerStatus.Running);
            }
            catch (Exception ex)
            {
                SysTray.This.ServiceStateChanged(ServiceControllerStatus.Stopped);
            }
        }

        static public void Unsubscribe()
        {
            lock (instanceContext)
            {
                try
                {
                    _this?.Unsubscribe();
                }
                catch (Exception e)
                {
                    Log.Main.Warning2(e);
                }
            }
        }

        static public Cliver.CisteraScreenCaptureService.Settings.GeneralSettings GetServiceSettings(out string __file)
        {
            __file = null;
            try
            {
                lock (instanceContext)
                {
                    trySubscribeNewInstance();
                    return _this?.GetSettings(out __file);
                }
            }
            catch (Exception e)
            {
                Log.Main.Warning2(e);
                BeginWatchingService();
            }
            return null;
        }

        static public string GetServiceLogDir()
        {
            try
            {
                lock (instanceContext)
                {
                    trySubscribeNewInstance();
                    return _this?.GetLogDir();
                }
            }
            catch (Exception e)
            {
                Log.Main.Warning2(e);
                BeginWatchingService();
            }
            return null;
        }

        static public void StartStopService(bool start)
        {
            lock (instanceContext)
            {
                try
                {
                    //if(!WindowsUserRoutines.CurrentUserHasElevatedPrivileges())
                    if (!ProcessRoutines.ProcessHasElevatedPrivileges())
                    {
                        if (Message.YesNo("This action requires elevated privileges. Would you like to restart this application 'As Administrator'?"))
                            ProcessRoutines.Restart(true);
                        return;
                    }

                    double timeoutSecs = 20;
                    using (ServiceController serviceController = new ServiceController(SERVICE_NAME))
                    {
                        if (start)
                        {
                            serviceController.Start();
                            serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeoutSecs));
                            if (serviceController.Status != ServiceControllerStatus.Running)
                                Message.Error("Could not start service '" + SERVICE_NAME + "' within " + timeoutSecs + " secs.");
                            else
                                //SysTray.This.ServiceStateChanged(ServiceControllerStatus.Running);
                                trySubscribeNewInstance();
                        }
                        else
                        {
                            serviceController.Stop();
                            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSecs));
                            if (serviceController.Status != ServiceControllerStatus.Stopped)
                                Message.Error("Could not stop service '" + SERVICE_NAME + "' within " + timeoutSecs + " secs.");
                            else
                                SysTray.This.ServiceStateChanged(ServiceControllerStatus.Stopped);
                        }
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
        }

        static public ServiceControllerStatus? GetServiceStatus()
        {
            lock (instanceContext)
            {
                try
                {
                    using (ServiceController serviceController = new ServiceController(SERVICE_NAME))
                    {
                        return serviceController.Status;
                    }
                }
                catch (Exception e)
                {
                    LogMessage.Error(e);
                }
                return null;
            }
        }
        public const string SERVICE_NAME = "Cistera Screen Capture Service";
    }
}
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
using System.ServiceProcess;
using Cliver.CisteraScreenCaptureService;
using System.Runtime.InteropServices;

namespace Cliver.CisteraScreenCaptureUI
{
    [CallbackBehaviorAttribute(UseSynchronizationContext = true)]
    public class UiApiCallback : CisteraScreenCaptureService.IUiApiCallback
    {
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
#region testing
        static internal void testCreateInstanceContext()
        {
            _this = new CisteraScreenCaptureService.UiApiClient(instanceContext);
        }
        static internal void testCloseInstanceContext()
        {
            if (_this != null)
            {
                _this.Unsubscribe();
                _this.Close();
                _this = null;
            }
        }
        static internal void testSubscribe()
        {
            _this.Subscribe();
        }
#endregion

        static UiApiClient()
        {
            try
            {
                instanceContext = new InstanceContext(new UiApiCallback());
                
                System.ServiceProcess.ServiceControllerStatus? status = UiApiClient.GetServiceStatus();
                if (status != null && status != System.ServiceProcess.ServiceControllerStatus.Stopped)
                    _this = new CisteraScreenCaptureService.UiApiClient(instanceContext);

                beginMonitorServiceStartStop();
            }
            catch (Exception e)
            {
                LogMessage.Error(e);
            }
        }
        readonly static InstanceContext instanceContext;
        static CisteraScreenCaptureService.UiApiClient _this = null;

        static void beginMonitorServiceStartStop()
        {
            try
            {
                IntPtr hSCM = WinApi.Advapi32.OpenSCManager(null, null, WinApi.Advapi32.SCM_ACCESS.SC_MANAGER_CONNECT);//(WinApi.Advapi32.SCM_ACCESS)0xF003F);// 
                if (hSCM == IntPtr.Zero)
                    throw new Exception("OpenSCManager: " + ErrorRoutines.GetLastError());
                IntPtr hService = WinApi.Advapi32.OpenService(hSCM, Cliver.CisteraScreenCaptureService.Program.SERVICE_NAME, WinApi.Advapi32.OpenServiceDesiredAccess.SERVICE_QUERY_STATUS);
                if (hService == IntPtr.Zero)
                    throw new Exception("OpenService: " + ErrorRoutines.GetLastError());
                ThreadRoutines.StartTry(() =>
                {
                    for (; ; )
                    {
                        serviceSatusChangedNotify = new WinApi.Advapi32.SERVICE_NOTIFY();
                        serviceSatusChangedNotify.dwVersion = 2;
                        serviceSatusChangedNotify.pfnNotifyCallback = Marshal.GetFunctionPointerForDelegate(serviceStatusChangedDelegate);
                        serviceSatusChangedNotify.pContext = IntPtr.Zero;
                        serviceSatusChangedNotify.dwNotificationStatus = 0;
                        WinApi.Advapi32.SERVICE_STATUS_PROCESS process;
                        process.dwServiceType = 0;
                        process.dwCurrentState = 0;
                        process.dwControlsAccepted = 0;
                        process.dwWin32ExitCode = 0;
                        process.dwServiceSpecificExitCode = 0;
                        process.dwCheckPoint = 0;
                        process.dwWaitHint = 0;
                        process.dwProcessId = 0;
                        process.dwServiceFlags = 0;
                        serviceSatusChangedNotify.ServiceStatus = process;
                        serviceSatusChangedNotify.dwNotificationTriggered = 0;
                        serviceSatusChangedNotify.pszServiceNames = Marshal.StringToHGlobalUni(Cliver.CisteraScreenCaptureService.Program.SERVICE_NAME);
                        notifyHandle = GCHandle.Alloc(serviceSatusChangedNotify, GCHandleType.Pinned);
                        unmanagedNotifyStructure = notifyHandle.AddrOfPinnedObject();
                        if (0 != WinApi.Advapi32.NotifyServiceStatusChange(hService, WinApi.Advapi32.NotifyMask.SERVICE_NOTIFY_RUNNING | WinApi.Advapi32.NotifyMask.SERVICE_NOTIFY_STOPPED, unmanagedNotifyStructure))
                            LogMessage.Error("NotifyServiceStatusChange: " + ErrorRoutines.GetLastError());

                        serviceStatusChangedManualResetEvent.Reset();
                        serviceStatusChangedManualResetEvent.WaitOne();
                        notifyHandle.Free();
                    }
                },
                null,
                () =>
                {
                    try
                    {
                        notifyHandle.Free();
                    }
                    catch { }
                }
                );
            }
            catch (Exception e)
            {
                LogMessage.Error(e);
            }
        }
        public static void ServiceStatusChanged(IntPtr parameter)
        {
            try
            {
                WinApi.Advapi32.SERVICE_NOTIFY n = Marshal.PtrToStructure<WinApi.Advapi32.SERVICE_NOTIFY>(parameter);
                //Log.Main.Trace("Service status: " + n.ServiceStatus.dwCurrentState);
                if (serviceStatus == n.ServiceStatus.dwCurrentState)//on some Windows ServiceStatusChanged is invoked many times!
                    return;
                serviceStatus = n.ServiceStatus.dwCurrentState;
                ServiceControllerStatus status;
                switch (n.ServiceStatus.dwCurrentState)
                {
                    case 1://stopped
                        status = ServiceControllerStatus.Stopped;
                        _this = null;
                        break;
                    case 4://running
                        status = ServiceControllerStatus.Running;
                        lock (instanceContext)
                        {
                            _this?.Unsubscribe();
                            _this?.Close();
                            _this = new CisteraScreenCaptureService.UiApiClient(instanceContext);
                            _this.Subscribe();
                            //beginKeepAliveServiceConnection();//it seems to be redundant because of infinite timeout, but sometimes the channel gets closed due to errors
                        }
                        break;
                    default:
                        throw new Exception("Unknown option: " + n.ServiceStatus.dwCurrentState);
                }
                Log.Main.Inform("Service status: " + status);
                SysTray.This.ServiceStateChanged(status);
            }
            catch (Exception e)
            {
                Log.Main.Error(e);
            }
            finally
            {
                serviceStatusChangedManualResetEvent.Set();
            }
        }
        static uint serviceStatus = 0;
        static private readonly ManualResetEvent serviceStatusChangedManualResetEvent = new ManualResetEvent(false);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void StatusChanged(IntPtr parameter);
        readonly public static StatusChanged serviceStatusChangedDelegate = ServiceStatusChanged;
        public static WinApi.Advapi32.SERVICE_NOTIFY serviceSatusChangedNotify;
        public static GCHandle notifyHandle;
        public static IntPtr unmanagedNotifyStructure;

        static void beginKeepAliveServiceConnection()
        {//it seems to be redundant because of infinite timeout, but sometimes the channel gets closed due to errors
            lock (instanceContext)
            {
                if (keepAliveServiceConnection_t != null && keepAliveServiceConnection_t.IsAlive)
                    return;
                keepAliveServiceConnection_t = ThreadRoutines.StartTry(
                    () =>
                    {
                        do
                        {
                            try
                            {
                                lock (instanceContext)
                                {
                                    _this.Subscribe();
                                }
                                Thread.Sleep(Settings.View.ServiceConnectionKeepAlivePulseTimeInMss);
                            }
                            catch (Exception e)
                            {
                                return;
                            }
                        }
                        while (_this != null && _this.State == CommunicationState.Opened && Thread.CurrentThread == keepAliveServiceConnection_t);
                    },
                    (Exception e) =>
                    {
                        LogMessage.Error(e);
                    },
                    null
                    );
            }
        }
        static Thread keepAliveServiceConnection_t = null;

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

        static public Cliver.CisteraScreenCaptureService.Settings.GeneralSettings GetServiceSettings(bool reset = false)
        {
            try
            {
                lock (instanceContext)
                {
                    return _this?.GetSettings(reset);
                }
            }
            catch (Exception e)
            {
                Log.Main.Warning2(e);
            }
            return null;
        }

        static public void SaveServiceSettings(Cliver.CisteraScreenCaptureService.Settings.GeneralSettings general)
        {
            try
            {
                lock (instanceContext)
                {
                    _this?.SaveSettings(general);
                }
            }
            catch (Exception e)
            {
                Log.Main.Warning2(e);
            }
        }

        static public string GetServiceLogDir()
        {
            try
            {
                lock (instanceContext)
                {
                    return _this?.GetLogDir();
                }
            }
            catch (Exception e)
            {
                Log.Main.Warning2(e);
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
                    if (!ProcessRoutines.ProcessHasElevatedPrivileges() && !ProcessRoutines.ProcessIsSystem()/*used for configuration during installing*/)
                    {
                        if (Message.YesNo("This action requires elevated privileges. Would you like to restart this application 'As Administrator'?"))
                            ProcessRoutines.Restart(true);
                        return;
                    }

                    double timeoutSecs = 20;
                    using (ServiceController serviceController = new ServiceController(Cliver.CisteraScreenCaptureService.Program.SERVICE_NAME))
                    {
                        if (start)
                        {
                            serviceController.Start();
                            serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeoutSecs));
                            if (serviceController.Status != ServiceControllerStatus.Running)
                                Message.Error("Could not start service '" + Cliver.CisteraScreenCaptureService.Program.SERVICE_NAME + "' within " + timeoutSecs + " secs.");
                        }
                        else
                        {
                            serviceController.Stop();
                            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSecs));
                            if (serviceController.Status != ServiceControllerStatus.Stopped)
                                Message.Error("Could not stop service '" + Cliver.CisteraScreenCaptureService.Program.SERVICE_NAME + "' within " + timeoutSecs + " secs.");
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
                    using (ServiceController serviceController = new ServiceController(Cliver.CisteraScreenCaptureService.Program.SERVICE_NAME))
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
    }
}
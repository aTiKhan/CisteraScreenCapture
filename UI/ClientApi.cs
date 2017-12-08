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

/// <summary>
/// TBD:
/// - add display area;
/// </summary>
/// 
namespace Cliver.CisteraScreenCaptureUI
{
    [CallbackBehaviorAttribute(UseSynchronizationContext = false)]
    public class UiApiCallback : ServiceConnection.IUiApiCallback
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
                InstanceContext context = new InstanceContext(new UiApiCallback());
                This = new ServiceConnection.UiApiClient(context);
                This.Subscribe();
            }
            catch (Exception e)
            {
                LogMessage.Error(e);
            }
        }
        readonly public static ServiceConnection.UiApiClient This = null;

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
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
    public class ServiceCallback : IClientApi
    {
        public void ServiceStatusChanged(System.ServiceProcess.ServiceControllerStatus status)
        {
            SysTray.This.ServiceStateChanged(status);
        }

        public void Message(MessageType messageType, string message)
        { }
    }

    public class ServiceApi
    {
        public static ServiceApi This = null;

        public void StartStop(bool start)
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
                Message.Error(ex);
            }
            finally
            {
            }
        }

        public ServiceControllerStatus? GetStatus()
        {
            try
            {
                ServiceController serviceController = new ServiceController(SERVICE_NAME);
                return serviceController.Status;
            }
            catch (Exception e)
            {
                Log.Main.Error(e);
            }
            return null;
        }
        public const string SERVICE_NAME = "CisteraScreenCapture";
    }
}
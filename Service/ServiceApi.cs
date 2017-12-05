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


/// <summary>
/// TBD:
/// - add display area;
/// </summary>
/// 
namespace Cliver.CisteraScreenCaptureService
{
    [ServiceContract(Namespace = "", SessionMode = SessionMode.Required, CallbackContract = typeof(IClientApi))]
    public interface IServiceApi
    {
        [OperationContract(IsOneWay = true, IsInitiating = true)]
        void Subscribe();

        [OperationContract(IsOneWay = true)]
        void Unsubscribe();

        [OperationContract()]
        Settings.GeneralSettings GetSettings();
    }

    public interface IClientApi
    {
        [OperationContract(IsOneWay = true)]
        void ServiceStatusChanged(System.ServiceProcess.ServiceControllerStatus status);

        [OperationContract(IsOneWay = true)]
        void Message(MessageType messageType, string message);
    }
    public enum MessageType
    {
        INFORM,
        WARNING,
        ERROR,
    }

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerSession)]
    public class ServiceApi : IServiceApi
    {
        delegate void statusChangedHandler(System.ServiceProcess.ServiceControllerStatus status);
        void statusChangedDelegate(System.ServiceProcess.ServiceControllerStatus status)
        {
            lock (serviceHost)
            {
                IClientApi serviceCallback = OperationContext.Current.GetCallbackChannel<IClientApi>();
                serviceCallback.ServiceStatusChanged(status);
            }
        }

        delegate void messageHandler(MessageType messageType, string message);
        void messageHandlerDelegate(MessageType messageType, string message)
        {
            lock (serviceHost)
            {
                IClientApi serviceCallback = OperationContext.Current.GetCallbackChannel<IClientApi>();
                serviceCallback.Message(messageType, message);
            }
        }

        public void Subscribe()
        {
            lock (serviceHost)
            {
                IClientApi serviceCallback = OperationContext.Current.GetCallbackChannel<IClientApi>();
                if (serviceCallbacks.Contains(serviceCallback))
                    return;
                serviceCallbacks.Add(serviceCallback);
            }
        }
        static readonly HashSet<IClientApi> serviceCallbacks = new HashSet<IClientApi>();

        public void Unsubscribe()
        {
            lock (serviceHost)
            {
                IClientApi serviceCallback = OperationContext.Current.GetCallbackChannel<IClientApi>();
                if (!serviceCallbacks.Contains(serviceCallback))
                    return;
                serviceCallbacks.Remove(serviceCallback);
            }
        }

        //public string[] GetSettingsPaths()
        //{
        //    lock (serviceHost)
        //    {
        //        return new string[] { Settings.General.__File };
        //    }
        //}
        public Settings.GeneralSettings GetSettings()
        {
            return Settings.General;
        }

        ServiceApi()
        {
        }

        static internal void OpenApi()
        {
            if (serviceHost == null)
            {
                lock (serviceHost)
                {
                    if (serviceHost != null)
                        return;
                    serviceHost = new ServiceHost(typeof(ServiceApi));
                    serviceHost.Open();
                }
            }
        }
        static ServiceHost serviceHost = null;

        //static internal ServerApi This
        //{
        //    get
        //    {
        //        return _this;
        //    }
        //}
        //static ServerApi _this = null;

        //static internal void CloseApi()
        //{
        //    if (serviceHost != null)
        //    {
        //        serviceHost.Close();
        //        serviceHost = null;
        //    }
        //}

        internal static void StatusChanged(System.ServiceProcess.ServiceControllerStatus status)
        {
            ThreadRoutines.StartTry(() =>
            {
                lock (serviceHost)
                {
                    foreach (IClientApi serviceCallback in serviceCallbacks)
                        serviceCallback.ServiceStatusChanged(status);
                }
            });
        }

        internal static void Message(MessageType messageType, string message)
        {
            ThreadRoutines.StartTry(() =>
            {
                lock (serviceHost)
                {
                    foreach (IClientApi serviceCallback in serviceCallbacks)
                        serviceCallback.Message(messageType, message);
                }
            });
        }
    }
}
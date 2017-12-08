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
    }

    public interface IUiApiCallback
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
    public class UiApi : IUiApi
    {
        delegate void statusChangedHandler(System.ServiceProcess.ServiceControllerStatus status);
        void statusChangedDelegate(System.ServiceProcess.ServiceControllerStatus status)
        {
            lock (lockObject)
            {
                IUiApiCallback uiApiCallback = OperationContext.Current.GetCallbackChannel<IUiApiCallback>();
                uiApiCallback.ServiceStatusChanged(status);
            }
        }

        delegate void messageHandler(MessageType messageType, string message);
        void messageHandlerDelegate(MessageType messageType, string message)
        {
            lock (lockObject)
            {
                IUiApiCallback uiApiCallback = OperationContext.Current.GetCallbackChannel<IUiApiCallback>();
                uiApiCallback.Message(messageType, message);
            }
        }

        public void Subscribe()
        {
            lock (lockObject)
            {
                IUiApiCallback uiApiCallback = OperationContext.Current.GetCallbackChannel<IUiApiCallback>();
                if (serviceCallbacks.Contains(uiApiCallback))
                    return;
                serviceCallbacks.Add(uiApiCallback);
            }
        }
        static readonly HashSet<IUiApiCallback> serviceCallbacks = new HashSet<IUiApiCallback>();

        public void Unsubscribe()
        {
            lock (lockObject)
            {
                IUiApiCallback uiApiCallback = OperationContext.Current.GetCallbackChannel<IUiApiCallback>();
                if (!serviceCallbacks.Contains(uiApiCallback))
                    return;
                serviceCallbacks.Remove(uiApiCallback);
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

        UiApi()
        {
        }

        static internal void OpenApi()
        {
            if (serviceHost == null)
            {
                lock (lockObject)
                {
                    if (serviceHost != null)
                        return;
                    Log.Main.Inform("Opening UI API.");
                    serviceHost = new ServiceHost(typeof(UiApi));
                    serviceHost.Open();
                }
            }
        }
        static ServiceHost serviceHost = null;
        static readonly object lockObject = new object();

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
                lock (lockObject)
                {
                    foreach (IUiApiCallback uiApiCallback in serviceCallbacks)
                        uiApiCallback.ServiceStatusChanged(status);
                }
            });
        }

        internal static void Message(MessageType messageType, string message)
        {
            ThreadRoutines.StartTry(() =>
            {
                lock (lockObject)
                {
                    foreach (IUiApiCallback uiApiCallback in serviceCallbacks)
                        uiApiCallback.Message(messageType, message);
                }
            });
        }
    }
}
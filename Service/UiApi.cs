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
        Settings.GeneralSettings GetSettings(out string __file);

        [OperationContract()]
        bool IsAlive();
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
        public void Subscribe()
        {
            lock (uiApiCallbacks)
            {
                IUiApiCallback uiApiCallback = OperationContext.Current.GetCallbackChannel<IUiApiCallback>();
                if (uiApiCallbacks.Contains(uiApiCallback))
                    return;
                uiApiCallbacks.Add(uiApiCallback);
                Log.Write("Subscibed: " + uiApiCallbacks.Count);
            }
        }
        static readonly HashSet<IUiApiCallback> uiApiCallbacks = new HashSet<IUiApiCallback>();

        public void Unsubscribe()
        {
            lock (uiApiCallbacks)
            {
                IUiApiCallback uiApiCallback = OperationContext.Current.GetCallbackChannel<IUiApiCallback>();
                uiApiCallbacks.Remove(uiApiCallback);
                Log.Write("Unsubscibed: " + uiApiCallbacks.Count);
            }
        }

        public Settings.GeneralSettings GetSettings(out string __file)
        {
            __file = Settings.General.__File;
            return Settings.General;
        }

        public bool IsAlive()
        {
            return true;
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

        internal static void StatusChanged(System.ServiceProcess.ServiceControllerStatus status)
        {
            Log.Write("Subscibed3: " + uiApiCallbacks.Count + status);
            ThreadRoutines.StartTry(() =>
            {
                Log.Write("Subscibed4: " + uiApiCallbacks.Count);
                lock (uiApiCallbacks)
                {
                    Log.Write("Subscibed5: " + uiApiCallbacks.Count);
                    foreach (IUiApiCallback uiApiCallback in uiApiCallbacks)
                    {
                        Log.Write("Subscibed6: " + uiApiCallbacks.Count);
                        uiApiCallback.ServiceStatusChanged(status);
                    }
                }
            });
        }

        internal static void Message(MessageType messageType, string message)
        {
            ThreadRoutines.StartTry(() =>
            {
                lock (uiApiCallbacks)
                {
                    foreach (IUiApiCallback uiApiCallback in uiApiCallbacks)
                        uiApiCallback.Message(messageType, message);
                }
            });
        }
    }
}
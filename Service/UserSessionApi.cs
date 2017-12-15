using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Cliver.CisteraScreenCaptureService
{
    [ServiceContract]
    public interface IUserSessionApi
    {
        //[OperationContract]
        //string GetCapturedMonitorDeviceName();

        //[OperationContract]
        //void SetCapturedMonitorDeviceName(WinApi.User32.RECT capturedMonitorRectangle);

        [OperationContract()]
        Settings.GeneralSettings GetSettings();

        [OperationContract()]
        void SaveSettings(Settings.GeneralSettings general);

        //[OperationContract()]
        //string GetSettingsFile();
    }

    public class UserSessionApi : IUserSessionApi
    {
        static internal void OpenApi()
        {
            if (serviceHost == null)
            {
                if (serviceHost != null)
                    return;
                Log.Main.Inform("Opening UserSession API.");
                
                serviceHost = new ServiceHost(typeof(UserSessionApi));
                serviceHost.Open();
            }
        }
        static ServiceHost serviceHost = null;

        static internal void CloseApi()
        {
            if (serviceHost != null)
            {
                Log.Main.Inform("Closing UserSession API.");
                serviceHost.Close();
                serviceHost = null;
            }
        }

        //public string GetCapturedMonitorDeviceName()
        //{
        //    return Settings.General.CapturedMonitorDeviceName;
        //}

        //public void SetCapturedMonitorDeviceName(WinApi.User32.RECT capturedMonitorRectangle)
        //{
        //    Settings.General.CapturedMonitorRectangle = capturedMonitorRectangle;
        //}

        public Settings.GeneralSettings GetSettings()
        {
            return Settings.General.GetReloadedInstance<Settings.GeneralSettings>();
        }

        public void SaveSettings(Settings.GeneralSettings general)
        {
            general.Save(Settings.General.__File);
        }

        //public string GetSettingsFile()
        //{
        //    return Settings.General.__File;
        //}
    }
}

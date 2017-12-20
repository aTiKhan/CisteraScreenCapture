using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceProcess;
using Cliver.CisteraScreenCaptureService;

namespace Cliver.CisteraScreenCaptureService.UserSessionProbe
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                LogMessage.DisableStumblingDialogs = true;
                Log.Initialize(Log.Mode.ONLY_LOG, Log.CompanyCommonDataDir);
                
                //string generalSettingsFile = args[1];
                //Cliver.CisteraScreenCaptureService.Settings.GeneralSettings general = Cliver.Settings.Create<Cliver.CisteraScreenCaptureService.Settings.GeneralSettings>(generalSettingsFile);

                Cliver.CisteraScreenCaptureService.Settings.GeneralSettings general = UserSessionApiClient.GetServiceSettings();
                Log.Main.Inform("Initial CapturedMonitorDeviceName: " + general.CapturedMonitorDeviceName);
                if (string.IsNullOrWhiteSpace(general.CapturedMonitorDeviceName))
                    general.CapturedMonitorDeviceName = Cliver.CisteraScreenCaptureService.MonitorRoutines.GetDefaultMonitorName();
                if (string.IsNullOrWhiteSpace(general.CapturedMonitorDeviceName))
                    throw new Exception("Could not detect default monitor.");

                Cliver.WinApi.User32.RECT? a = Cliver.CisteraScreenCaptureService.MonitorRoutines.GetMonitorAreaByMonitorName(general.CapturedMonitorDeviceName);
                if (a == null)
                {
                    string defaultMonitorName = Cliver.CisteraScreenCaptureService.MonitorRoutines.GetDefaultMonitorName();
                    LogMessage.Warning("Monitor '" + general.CapturedMonitorDeviceName + "' was not found. Using default one '" + defaultMonitorName + "'");
                    general.CapturedMonitorDeviceName = defaultMonitorName;
                    a = Cliver.CisteraScreenCaptureService.MonitorRoutines.GetMonitorAreaByMonitorName(general.CapturedMonitorDeviceName);
                    if (a == null)
                        throw new Exception("Monitor '" + general.CapturedMonitorDeviceName + "' was not found.");
                }
                general.CapturedMonitorRectangle = a;
                UserSessionApiClient.SaveServiceSettings(general);
                Log.Main.Inform("Finish CapturedMonitorDeviceName: " + general.CapturedMonitorDeviceName + "\r\nCapturedMonitorRectangle: " + general.CapturedMonitorRectangle.Value.Left + "," + general.CapturedMonitorRectangle.Value.Top + "," + general.CapturedMonitorRectangle.Value.Right + "," + general.CapturedMonitorRectangle.Value.Bottom);
            }
            catch (Exception e)
            {
                Log.Main.Error(e);
            }
        }
    }

    public partial class UserSessionApiClient
    {
        static UserSessionApiClient()
        {
            try
            {
                _this = new CisteraScreenCaptureService.UserSessionApiClient();
            }
            catch (Exception e)
            {
                LogMessage.Error(e);
            }
        }
        static CisteraScreenCaptureService.UserSessionApiClient _this;

        static public Cliver.CisteraScreenCaptureService.Settings.GeneralSettings GetServiceSettings()
        {
            try
            {
                return _this?.GetSettings();
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
                _this?.SaveSettings(general);
            }
            catch (Exception e)
            {
                Log.Main.Warning2(e);
            }
        }

        //static public string GetServiceSettings()
        //{
        //    try
        //    {
        //        return _this?.GetSettingsFile();
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Main.Warning2(e);
        //    }
        //    return null;
        //}

    }
}
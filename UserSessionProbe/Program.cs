using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserSessionProbe
{
    class Program
    {
        static void Main(string[] args)
        {  




            {
                Settings.General.CapturedMonitorDeviceName = MonitorRoutines.GetDefaultMonitorName();
                if (string.IsNullOrWhiteSpace(Settings.General.CapturedMonitorDeviceName))
                    throw new Exception("No monitor was found.");
            }
            WinApi.User32.RECT? an = MonitorRoutines.GetMonitorAreaByMonitorName(Settings.General.CapturedMonitorDeviceName);
            if (an == null)
            {
                string defaultMonitorName = MonitorRoutines.GetDefaultMonitorName();
                Log.Main.Warning("Monitor '" + Settings.General.CapturedMonitorDeviceName + "' was not found. Using default one '" + defaultMonitorName + "'");
                Settings.General.CapturedMonitorDeviceName = defaultMonitorName;
                an = MonitorRoutines.GetMonitorAreaByMonitorName(Settings.General.CapturedMonitorDeviceName);
                if (an == null)
                    throw new Exception("Monitor '" + Settings.General.CapturedMonitorDeviceName + "' was not found.");
            }
        }
    }
}

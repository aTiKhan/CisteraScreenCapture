//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//********************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceProcess;

namespace Cliver.CisteraScreenCaptureUI
{
    public partial class SysTray : Form
    {
        SysTray()
        {
            InitializeComponent();

            CreateHandle();

            Load += delegate
              {
                  Icon = AssemblyRoutines.GetAppIcon();
                  
                  ServiceStateChanged(UiApiClient.GetServiceStatus());
                  silentlyToolStripMenuItem.Checked = !Settings.View.DisplayNotifications;

                  string __file;
                  Cliver.CisteraScreenCaptureService.Settings.GeneralSettings general = UiApiClient.GetServiceSettings(out __file);
                  if(general.CapturedMonitorRectangle == null)
                  {
                      if(string.IsNullOrWhiteSpace( general.CapturedMonitorDeviceName))
                          general.CapturedMonitorDeviceName = Cliver.CisteraScreenCaptureService.MonitorRoutines.GetDefaultMonitorName();
                      if (string.IsNullOrWhiteSpace(general.CapturedMonitorDeviceName))
                          LogMessage.Error("Could not detect default monitor.");
                      else
                      {
                          WinApi.User32.RECT? a = Cliver.CisteraScreenCaptureService.MonitorRoutines.GetMonitorAreaByMonitorName(general.CapturedMonitorDeviceName);
                          if (a == null)
                          {
                              string defaultMonitorName = Cliver.CisteraScreenCaptureService.MonitorRoutines.GetDefaultMonitorName();
                              LogMessage.Warning("Monitor '" + general.CapturedMonitorDeviceName + "' was not found. Using default one '" + defaultMonitorName + "'");
                              general.CapturedMonitorDeviceName = defaultMonitorName;
                              a = Cliver.CisteraScreenCaptureService.MonitorRoutines.GetMonitorAreaByMonitorName(general.CapturedMonitorDeviceName);
                              if (a == null)
                                  LogMessage.Error("Monitor '" + general.CapturedMonitorDeviceName + "' was not found.");
                          }
                          general.CapturedMonitorRectangle = a;
                      }
                      general.Save(__file);
                  }
              };
        }

        public static readonly SysTray This = new SysTray();

        public void ServiceStateChanged(ServiceControllerStatus? status)
        {
            this.Invoke(() =>
            {
                StartStop.Checked = status == ServiceControllerStatus.Running;
                string title = "Cistera Screen Capture";
                if (StartStop.Checked)
                {
                    notifyIcon.Icon = Icon;
                    title += " started";
                }
                else
                {
                    //notifyIcon.Icon = Icon.FromHandle(ImageRoutines.GetGreyScale(Icon.ToBitmap()).GetHicon());
                    notifyIcon.Icon = Icon.FromHandle(ImageRoutines.GetInverted(Icon.ToBitmap()).GetHicon());
                    if(status != null)
                        title += " stopped";
                    else
                        title += " does not exist";
                }
                notifyIcon.Text = title;
            });
        }

        bool isAllowed()
        {
            try
            {
                if (WindowsUserRoutines.CurrentUserIsAdministrator())
                    return true;
                throw new System.Security.SecurityException();
            }
            catch (Exception e)
            {
                if (e is System.Security.SecurityException)
                    Message.Exclaim("This action is permitted for Administrators only.");
                else
                    LogMessage.Error(e);
            }
            return false;
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            settingsToolStripMenuItem_Click(null, null);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!isAllowed())
                return;
            SettingsWindow.Open();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //AboutForm.Open();
            AboutWindow.Open();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!isAllowed())
                return;
            if (!Message.YesNo("Exiting the UI while the service iteslf will remain. Proceed?", null, Message.Icons.Exclamation))
                return;
            //Program.Exit();
            Environment.Exit(0);
        }

        private void SysTray_VisibleChanged(object sender, EventArgs e)
        {
            this.Visible = false;
        }

        private void notifyIcon1_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void StartStop_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            //settingsToolStripMenuItem_Click(null, null);
        }

        private void StartStop_Click(object sender, EventArgs e)
        {
            if (!isAllowed())
            {
                StartStop.Checked = UiApiClient.GetServiceStatus() == ServiceControllerStatus.Running;
                return;
            }
            UiApiClient.StartStopService(StartStop.Checked);
            StartStop.Checked = UiApiClient.GetServiceStatus() == ServiceControllerStatus.Running;
        }

        private void stateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //List<string> ls = new List<string>();
            //ls.Add("Monitor: " + (Service.Running ? "started" : "stopped"));
            //ls.Add("(The values are fresh and may differ from those used by Service last time.)");
            //ls.Add("Logged in user: " + Service.GetUserName());

            ////var domains = ZeroconfResolver.BrowseDomainsAsync().Result;
            ////var responses = ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key)).Result;
            ////IReadOnlyList<IZeroconfHost> zhs = await ZeroconfResolver.ResolveAsync("_printer._tcp.local.");//worked for server: "_printer._tcp"
            //string service = Settings.General.GetServiceName();
            //IReadOnlyList<IZeroconfHost> zhs = ZeroconfResolver.ResolveAsync(service, TimeSpan.FromSeconds(3), 1, 10).Result;
            //string server_ip;
            //if (zhs.Count < 1)
            //{
            //    server_ip = Settings.General.TcpClientDefaultIp.ToString();
            //    ls.Add("Service '" + service + "' could not be resolved. Using default ip: " + server_ip);
            //}
            //else if (zhs.Where(x => x.IPAddress != null).FirstOrDefault() == null)
            //{
            //    server_ip = Settings.General.TcpClientDefaultIp.ToString();
            //    ls.Add("Resolution of service '" + service + "' has no IP defined. Using default ip: " + server_ip);
            //}
            //else
            //    ls.Add("Service '" + service + "' has been resolved to: " + zhs.Where(x => x.IPAddress != null).FirstOrDefault().IPAddress);

            //if (!TcpServer.Running)
            //    ls.Add("Tcp listening: -");
            //else
            //    ls.Add("Tcp listening on: " + TcpServer.LocalIp + ":" + TcpServer.LocalPort);

            //if (TcpServer.Connection == null)
            //    ls.Add("Tcp connection: -");
            //else
            //    ls.Add("Tcp connection to: " + TcpServer.Connection.RemoteIp + ":" + TcpServer.Connection.RemotePort);

            //if (!MpegStream.Running)
            //    ls.Add("Mpeg stream: -");
            //else
            //    ls.Add("Mpeg stream: " + MpegStream.CommandLine);

            //Message.Inform(string.Join("\r\n", ls));
        }

        private void silentlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.View.DisplayNotifications = !silentlyToolStripMenuItem.Checked;
            Settings.View.Save();
        }

        private void serviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string d = UiApiClient.GetServiceLogDir();
                if (d == null)
                    Message.Error("The service is unavailable.");
                else
                    Process.Start(d);
            }
            catch (Exception ex)
            {
                LogMessage.Error(ex);
            }
        }

        private void uIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(Log.WorkDir);
            }
            catch(Exception ex)
            {
                LogMessage.Error(ex);
            }
        }
    }
}
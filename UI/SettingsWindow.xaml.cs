//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//********************************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Management;
using System.Threading;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Cliver.CisteraScreenCaptureService;

namespace Cliver.CisteraScreenCaptureUI
{
    public partial class SettingsWindow : Window
    {
        SettingsWindow()
        {
            InitializeComponent();
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);

            Icon = AssemblyRoutines.GetAppIconImageSource();

            ContentRendered += delegate
            {
                //this.MinHeight = this.ActualHeight;
                //this.MaxHeight = this.ActualHeight;
                //this.MinWidth = this.ActualWidth;
            };

            WpfRoutines.AddFadeEffect(this, 300);

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            //WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //DefaultServerIp.ValueDataType = typeof(IPAddress);

            general = UiApiClient.GetServiceSettings();
            if (general == null)
            {
                ok.IsEnabled = false;
                reset.IsEnabled = false;
                Message.Error("The service is unavailable.");
                return;
            }
            set();

            if (!ProcessRoutines.ProcessHasElevatedPrivileges() && !ProcessRoutines.ProcessIsSystem()/*used for configuration during installing*/)
            {
                ok.IsEnabled = false;
                reset.IsEnabled = false;
                if (Message.YesNo("Settings modification requires elevated privileges. Would you like to restart this application 'As Administrator'?"))
                    ProcessRoutines.Restart(true);
            }
        }
        Cliver.CisteraScreenCaptureService.Settings.GeneralSettings general;

        void set()
        {
            //ServerDefaultPort.Text = general.TcpClientDefaultPort.ToString();
            ServerDefaultIp.Text = general.TcpClientDefaultIp.ToString();
            ClientPort.Text = general.TcpServerPort.ToString();
            ServiceDomain.Text = general.ServiceDomain;
            ServiceType.Text = general.ServiceType;

            //using (ManagementObjectSearcher monitors = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor"))
            //{
            //    foreach (ManagementObject monitor in monitors.Get())
            //    {
            //        MonitorName.Items.Add(monitor["Name"].ToString() + "|" + monitor["DeviceId"].ToString());// + "(" + monitor["ScreenHeight"].ToString() +"x"+ monitor["ScreenWidth"].ToString() + ")");
            //    }
            //}
            //foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            //{
            //    // For each screen, add the screen properties to a list box.
            //    MonitorName.Items.Add("Device Name: " + screen.DeviceName);
            //    MonitorName.Items.Add("Bounds: " + screen.Bounds.ToString());
            //    //MonitorName.Items.Add("Type: " + screen.GetType().ToString());
            //    //MonitorName.Items.Add("Working Area: " + screen.WorkingArea.ToString());
            //    MonitorName.Items.Add("Primary Screen: " + screen.Primary.ToString());
            //}
            Monitors.DisplayMemberPath = "Text";
            Monitors.SelectedValuePath = "Value";
            Monitors.Items.Add(new
            {
                Text = Cliver.CisteraScreenCaptureService.Settings.GeneralSettings.CapturedMonitorDeviceName_ALL_DISPLAYS,
                Value = Cliver.CisteraScreenCaptureService.Settings.GeneralSettings.CapturedMonitorDeviceName_ALL_DISPLAYS,
            });
            foreach (MonitorRoutines.MonitorInfo mi in MonitorRoutines.GetMonitorInfos())
            {
                Monitors.Items.Add(new
                {
                    Text = mi.DeviceString + " (" + (mi.Area.Bottom - mi.Area.Top) + "x" + (mi.Area.Right - mi.Area.Left) + ")",
                    Value = mi.DeviceName
                });
            }
            //if (Monitors.Items.Count < 1)
            //    throw new Exception("No monitor was found!");
            if (general.CapturedMonitorDeviceName != null)
                Monitors.SelectedValue = general.CapturedMonitorDeviceName;
            else
                Monitors.SelectedValue = 0;
            //if (Monitors.SelectedIndex < 0)
            //    Monitors.SelectedIndex = 0;

            ShowMpegWindow.IsChecked = general.ShowMpegWindow;
            WriteMpegOutput2Log.IsChecked = general.WriteMpegOutput2Log;

            DeleteLogsOlderDays.Text = general.DeleteLogsOlderDays.ToString();
        }

        static public void Open()
        {
            lock (lockObject)
            {
                if (w == null)
                {
                    w = new SettingsWindow();
                    w.Closed += delegate
                    {
                        w = null;
                    };
                }
                w.Show();
                w.Activate();
            }
        }
        static SettingsWindow w = null;
        readonly static object lockObject = new object();

        static public void OpenDialog()
        {
            lock (lockObject)
            {
                if (w == null)
                {
                    w = new SettingsWindow();
                    w.Closed += delegate
                    {
                        w = null;
                    };
                }
                w.ShowDialog();
            }
        }

        static bool RequireElevatedRightsToManage = true;
        void close(object sender, EventArgs e)
        {
            Close();
        }

        void save(object sender, EventArgs e)
        {
            try
            {
                ushort v;

                //if (!ushort.TryParse(ServerDefaultPort.Text, out v))
                //    throw new Exception("Server port must be an integer between 0 and " + ushort.MaxValue);
                //general.TcpClientDefaultPort = v;

                if (string.IsNullOrWhiteSpace(ServerDefaultIp.Text))
                    throw new Exception("Default server ip is not specified.");
                IPAddress ia;
                if (!IPAddress.TryParse(ServerDefaultIp.Text, out ia))
                    throw new Exception("Default server ip is not a valid value.");
                general.TcpClientDefaultIp = ia.ToString();

                if (!ushort.TryParse(ClientPort.Text, out v))
                    throw new Exception("Client port must be an between 0 and " + ushort.MaxValue);
                general.TcpServerPort = v;

                if (string.IsNullOrWhiteSpace(ServiceDomain.Text))
                    throw new Exception("Service domian is not specified.");
                general.ServiceDomain = ServiceDomain.Text.Trim();

                if (string.IsNullOrWhiteSpace(ServiceType.Text))
                    throw new Exception("Service type is not specified.");
                general.ServiceType = ServiceType.Text.Trim();

                if (Monitors.SelectedIndex < 0)
                    throw new Exception("Captured Video Source is not specified.");
                general.CapturedMonitorDeviceName = (string)Monitors.SelectedValue;

                general.ShowMpegWindow = ShowMpegWindow.IsChecked ?? false;

                general.WriteMpegOutput2Log = WriteMpegOutput2Log.IsChecked ?? false;

                //general.CapturedMonitorRectangle = MonitorRoutines.GetMonitorAreaByMonitorName(general.CapturedMonitorDeviceName);
                //if (general.CapturedMonitorRectangle == null)
                //    throw new Exception("Could not get rectangle for monitor '" + general.CapturedMonitorDeviceName + "'");
                general.CapturedMonitorRectangle = null;
                
                general.DeleteLogsOlderDays = int.Parse(DeleteLogsOlderDays.Text);

                UiApiClient.SaveServiceSettings(general);

                if (Settings.View.DeleteLogsOlderDays != general.DeleteLogsOlderDays)
                {
                    Settings.View.DeleteLogsOlderDays = general.DeleteLogsOlderDays;
                    Settings.View.Save();
                }

                System.ServiceProcess.ServiceControllerStatus? status = UiApiClient.GetServiceStatus();
                if (status != null && status != System.ServiceProcess.ServiceControllerStatus.Stopped
                    && (ProcessRoutines.ProcessIsSystem()/*used for configuration during installing*/
                    || Message.YesNo("The changes have been saved and will be engaged after service restart. Would you like to restart the service (all the present connections if any, will be terminated)?")
                    )
                    )
                {
                    MessageForm mf = null;
                    ThreadRoutines.StartTry(() =>
                    {
                        mf = new MessageForm(System.Windows.Forms.Application.ProductName, System.Drawing.SystemIcons.Information, "Resetting the service. Please wait...", null, 0, null);
                        mf.ShowDialog();
                    });

                    UiApiClient.StartStopService(false);
                    UiApiClient.StartStopService(true);

                    if (null == SleepRoutines.WaitForObject(() => { return mf; }, 1000))
                        throw new Exception("Could not get MessageForm");
                    mf.Invoke(() => { mf.Close(); });
                }

                Close();
            }
            catch (Exception ex)
            {
                Message.Exclaim(ex.Message);
            }
        }

        //class ValidationException:Exception
        //{

        //}
               
        void reset_settings(object sender, RoutedEventArgs e)
        {
            //if (!Message.YesNo("Settings will be reset to their initial state. Proceed?"))
            //    return;
            //general.Reset();
            //general = Cliver.CisteraScreenCaptureService.Settings.General.GetResetInstance<Cliver.CisteraScreenCaptureService.Settings.GeneralSettings>();
            general = UiApiClient.GetServiceSettings(true);
            set();
        }
    }
}

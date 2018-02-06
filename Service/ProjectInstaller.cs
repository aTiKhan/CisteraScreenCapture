//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//********************************************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Cliver.CisteraScreenCaptureService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            this.Committed += delegate
            {
                //try
                //{
                ServiceController sc = new ServiceController(Program.SERVICE_NAME);
                sc.Start();
                //}
                //catch (Exception e)
                //{
                //    //Message.Error(ex);//brings to an error: object is null
                //    System.Windows.Forms.MessageBox.Show(e.Message);
                //}
            };

            this.BeforeUninstall += delegate
            {
                //try
                //{
                ServiceController sc = new ServiceController(Program.SERVICE_NAME);
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    double timeoutSecs = 20;
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSecs));
                    if (sc.Status != ServiceControllerStatus.Stopped)
                        throw new Exception("Could not stop service '" + Cliver.CisteraScreenCaptureService.Program.SERVICE_NAME + "'. To unistall it, stop it manually.");
                }
                //}
                //catch (Exception e)
                //{
                //    //Message.Error(ex);//brings to an error: object is null
                //    MessageBox.Show();
                //    throw e;//to stop uninstalling(?)
                //}

                try
                {
                    string servicePath = this.Context.Parameters["assemblypath"];
                    AssemblyRoutines.AssemblyInfo ai = new AssemblyRoutines.AssemblyInfo(servicePath);
                    WindowsFirewall.DeleteRule(ai.AssemblyProduct, servicePath);

                    WindowsFirewall.DeleteRule(ffmpegFirewallRuleName, PathRoutines.GetDirFromPath(servicePath) + "\\ffmpeg.exe");
                }
                catch (Exception e)
                {
                    //MessageBox.Show("You'll may need to set firewall manually because of the following error that happened while setting firewall:\r\n" + e.Message, "Cistera Screen Capture", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    //    throw e;
                }
            };

            this.AfterInstall += delegate
            {
                try
                {
                    string servicePath = this.Context.Parameters["assemblypath"];
                    AssemblyRoutines.AssemblyInfo ai = new AssemblyRoutines.AssemblyInfo(servicePath);
                    WindowsFirewall.DeleteRule(ai.AssemblyProduct);
                    WindowsFirewall.AllowProgram(ai.AssemblyProduct, servicePath, WindowsFirewall.Direction.IN);
                    //WindowsFirewall.AllowProgram(ai.AssemblyProduct, servicePath, WindowsFirewall.Direction.OUT);

                    WindowsFirewall.DeleteRule(ffmpegFirewallRuleName);
                    WindowsFirewall.AllowProgram(ffmpegFirewallRuleName, PathRoutines.GetDirFromPath(servicePath) + "\\ffmpeg.exe", WindowsFirewall.Direction.OUT);
                }
                catch (Exception e)
                {
                    MessageBox.Show("You'll may need to set firewall manually because of the following error that happened while setting firewall:\r\n" + e.Message, "Cistera Screen Capture", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    //    throw e;
                }
            };
        }

        const string ffmpegFirewallRuleName = "Ffmpeg";
    }
}

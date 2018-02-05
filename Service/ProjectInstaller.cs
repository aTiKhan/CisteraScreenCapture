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
                //    MessageBox.Show();
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
            };

            this.AfterInstall += delegate
            {
                AssemblyRoutines.AssemblyInfo ai = new AssemblyRoutines.AssemblyInfo();
                openFirewallForProgram(Process.GetCurrentProcess().MainModule.FileName, ai.AssemblyProduct);
            };
        }

        static public void test()
        {
            AssemblyRoutines.AssemblyInfo ai = new AssemblyRoutines.AssemblyInfo();
            openFirewallForProgram(Process.GetCurrentProcess().MainModule.FileName, ai.AssemblyProduct);
        }

        private static void openFirewallForProgram(string exeFileName, string displayName)
        {
            Process p = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "firewall add allowedprogram program=\"" + exeFileName + "\" name=\"" + displayName + "\" profile=\"ALL\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });
            string error = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (!string.IsNullOrEmpty(error))
                throw new Exception(error);            
        }
    }    
}

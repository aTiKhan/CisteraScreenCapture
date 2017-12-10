using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace Cliver.CisteraScreenCaptureService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }

        protected override void OnCommitted(System.Collections.IDictionary savedState)
        {
            try
            {
                ServiceController sc = new ServiceController(Program.SERVICE_NAME);
                sc.Start();
            }
            catch(Exception e)
            {
                Message.Error(e);
            }
        }
    }
}

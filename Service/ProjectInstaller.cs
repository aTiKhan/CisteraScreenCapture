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

            this.Committed += delegate
            {
                try
                {
                    ServiceController sc = new ServiceController(Program.SERVICE_NAME);
                    sc.Start();
                }
                catch (Exception e)
                {
                    Message.Error(e);
                }
            };

            this.BeforeUninstall += delegate
            {
                try
                {
                    ServiceController sc = new ServiceController(Program.SERVICE_NAME);
                    if(sc.Status != ServiceControllerStatus.Stopped)
                        sc.Stop();
                }
                catch (Exception e)
                {
                    Message.Error(e);
                }
            };
        }
    }
}

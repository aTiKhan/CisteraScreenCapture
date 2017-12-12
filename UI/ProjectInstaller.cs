using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace Cliver.CisteraScreenCaptureUI
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
                    //Process.Start(PathRoutines.GetDirFromPath(Assembly.GetExecutingAssembly().Location) + "\\InstallSample.exe");
                    Process.Start(Assembly.GetExecutingAssembly().Location);
                }
                catch (Exception ex)
                {
                    Message.Error(ex);
                }
            };
        }
    }
}

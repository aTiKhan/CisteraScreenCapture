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
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

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
                    //Process.Start(Assembly.GetExecutingAssembly().Location);
                    ProcessRoutines.CreateProcessAsUserOfCurrentSession(Assembly.GetExecutingAssembly().Location);//starts the process as the current user while the installer runs as SYSTEM
                }
                catch (Exception ex)
                {
                    Message.Error(ex);
                }
            };

            this.BeforeUninstall += delegate
            {
                try
                {
                    foreach (Process p in Process.GetProcessesByName(PathRoutines.GetFileNameWithoutExtentionFromPath(Assembly.GetExecutingAssembly().Location)))
                        ProcessRoutines.KillProcessTree(p.Id);
                }
                catch (Exception e)
                {
                    Message.Error(e);
                    //throw e;//to stop uninstalling(?)
                }
            };
        }
    }
}

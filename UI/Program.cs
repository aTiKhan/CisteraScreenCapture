//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//********************************************************************************************

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Generic;
using System.Net.Mail;
using Cliver;
using System.Configuration;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.ServiceProcess;

/// <summary>
/// TBD:
/// - add display area;
/// </summary>
/// 
namespace Cliver.CisteraScreenCaptureUI
{
    public class Program
    {
        static Program()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args)
            {
                Exception e = (Exception)args.ExceptionObject;
                LogMessage.Error(e);
                Exit();
            };

            Message.TopMost = true;

            Cliver.Config.Reload();
            LogMessage.DisableStumblingDialogs = false;
            Log.ShowDeleteOldLogsDialog = false;
            Log.Initialize(Log.Mode.ONLY_LOG, Log.CompanyCommonDataDir, true, Settings.View.DeleteLogsOlderDays);
        }

        public class CommandLineParameters : ProgramRoutines.CommandLineParameters
        {
            public static readonly CommandLineParameters INITIAL_CONFIGURATION = new CommandLineParameters("-initial_configuration");
            //public static readonly CommandLineParameters START = new CommandLineParameters("-start");
            //public static readonly CommandLineParameters STOP = new CommandLineParameters("-stop");
            //public static readonly CommandLineParameters EXIT = new CommandLineParameters("-exit");

            public CommandLineParameters(string value) : base(value) { }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Cliver.Wpf.Message.Inform("test");
                Log.Main.Inform("Version: " + AssemblyRoutines.GetAppVersion());
                string user = ProcessRoutines.GetProcessUserName();
                string m = "User: " + user;
                if (ProcessRoutines.ProcessHasElevatedPrivileges())
                    m += " (as administrator)";
                Log.Main.Inform(m);

                ProcessRoutines.RunSingleProcessOnly();

                if(ProgramRoutines.IsParameterSet<CommandLineParameters>(CommandLineParameters.INITIAL_CONFIGURATION))
                {
                    Message.Inform("Please configure service in the next window.");
                    SettingsWindow.OpenDialog();
                    return;
                }

#if !test
                Application.Run(SysTray.This);
#else
                UiApiClient.testCreateInstanceContext();
                SettingsWindow.Open();
                Application.Run(SysTray.This);
                //UiApiClient.testSubscribe();
                //UiApiClient.testSubscribe();
                //Thread.Sleep(1000);
                //UiApiClient.testCloseInstanceContext();
                //UiApiClient.testCreateInstanceContext();
                //UiApiClient.testSubscribe();
                //UiApiClient.testSubscribe();
                //for (; ; )
                //{
                //    System.Threading.Thread.Sleep(10000);
                //}
#endif
            }
            catch (Exception e)
            {
                LogMessage.Error(e);
            }
            finally
            {
                Exit();
            }
        }

        static internal void Exit()
        {
            Log.Main.Inform("Exiting...\r\n" + Log.GetStackString(0, 10));
            UiApiClient.Unsubscribe();
            Environment.Exit(0);
        }
    }
}
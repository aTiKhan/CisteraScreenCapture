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
                Message.Error(e);
                UiApiClient.Unsubscribe();
                Application.Exit();
            };

            Message.TopMost = true;

            LogMessage.DisableStumblingDialogs = false;
            Log.Initialize(Log.Mode.ONLY_LOG, Log.CompanyCommonDataDir);
            Cliver.Config.Reload();
        }

        //public class CommandLineParameters : ProgramRoutines.CommandLineParameters
        //{
        //    public static readonly CommandLineParameters START = new CommandLineParameters("-start");
        //    public static readonly CommandLineParameters STOP = new CommandLineParameters("-stop");
        //    public static readonly CommandLineParameters EXIT = new CommandLineParameters("-exit");

        //    public CommandLineParameters(string value) : base(value) { }
        //}

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Log.Main.Inform("Version: " + AssemblyRoutines.GetAppVersion());
                string user = ProcessRoutines.GetProcessUserName();
                string m = "User: " + user;
                if (ProcessRoutines.ProcessHasElevatedPrivileges())
                    m += " (as administrator)";
                Log.Main.Inform(m);
                
                ProcessRoutines.RunSingleProcessOnly();

#if !test
                Application.Run(SysTray.This);
#else

                UiApiClient.testCreateInstanceContext();
                UiApiClient.testSubscribe();
                UiApiClient.testSubscribe();
                Thread.Sleep(1000);
                UiApiClient.testCloseInstanceContext();
                UiApiClient.testCreateInstanceContext();
                UiApiClient.testSubscribe();
                UiApiClient.testSubscribe();
                for (; ; )
                {
                    System.Threading.Thread.Sleep(10000);
                }
#endif
            }
            catch (Exception e)
            {
                Message.Error(e);
            }
            finally
            {
                UiApiClient.Unsubscribe();
                Environment.Exit(0);
            }
        }
    }
}
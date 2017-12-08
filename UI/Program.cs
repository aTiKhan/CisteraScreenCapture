//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//        27 February 2007
//Copyright: (C) 2007, Sergey Stoyan
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

            Log.Initialize(Log.Mode.ONLY_LOG);
            //Log.Initialize(Log.Mode.ONLY_LOG, Log.CliverSoftCommonDataDir);
            //Config.Initialize(new string[] { "General" });
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
                string m = "User: " + WindowsUserRoutines.GetCurrentUserName3() + "(";
                if (WindowsUserRoutines.CurrentUserIsAdministrator())
                {
                    m += "administrator";
                    if(WindowsUserRoutines.CurrentUserHasElevatedPrivileges())
                        m +=", elevated privileges";
                    else
                        m +=", not elevated privileges";
                }
                else
                    m += "not administrator";
                Log.Main.Inform(m + ")");

                ProcessRoutines.RunSingleProcessOnly();
                
                Application.Run(SysTray.This);
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
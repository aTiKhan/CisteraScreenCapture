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
//using System.Web.Script.Serialization;
using System.Collections.Generic;
using Cliver;
using System.Configuration;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Input;
using System.Net.Http;
using Zeroconf;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Cliver.CisteraScreenCaptureService
{
    public class MpegStream
    {
        public static void Start(uint sessionId, string arguments)
        {
            if(sessionId < 1)
                throw new Exception("sessionId == " + sessionId);

            if (mpeg_stream_process != null)
                Log.Main.Warning("The previous MpegStream was not stopped!");
            Stop();

            //if (string.IsNullOrWhiteSpace(Settings.General.CapturedMonitorDeviceName))
            //{
            //    Settings.General.CapturedMonitorDeviceName = MonitorRoutines.GetDefaultMonitorName();
            //    if (string.IsNullOrWhiteSpace(Settings.General.CapturedMonitorDeviceName))
            //        throw new Exception("No monitor was found.");
            //}
            //WinApi.User32.RECT? an = MonitorRoutines.GetMonitorAreaByMonitorName(Settings.General.CapturedMonitorDeviceName);
            //if (an == null)
            //{
            //    string defaultMonitorName = MonitorRoutines.GetDefaultMonitorName();
            //    Log.Main.Warning("Monitor '" + Settings.General.CapturedMonitorDeviceName + "' was not found. Using default one '" + defaultMonitorName + "'");
            //    Settings.General.CapturedMonitorDeviceName = defaultMonitorName;
            //    an = MonitorRoutines.GetMonitorAreaByMonitorName(Settings.General.CapturedMonitorDeviceName);
            //    if (an == null)
            //        throw new Exception("Monitor '" + Settings.General.CapturedMonitorDeviceName + "' was not found.");
            //}
            if (Settings.General.CapturedMonitorRectangle == null)
            {
                Log.Main.Inform("CapturedMonitorRectangle is empty. Running " + userSessionProbe);
                UserSessionApi.OpenApi();
                WinApi.Advapi32.CreationFlags cfs = 0;
                cfs |= WinApi.Advapi32.CreationFlags.CREATE_NO_WINDOW;
                string cl = "\"" + Log.AppDir + "\\" + userSessionProbe + "\"";
                uint pid = ProcessRoutines.CreateProcessAsUserOfParentProcess(sessionId, cl, cfs);
                Process p = Process.GetProcessById((int)pid);
                if (p != null && !p.HasExited)
                    p.WaitForExit();
                UserSessionApi.CloseApi();
                Settings.General.Reload();
                if (Settings.General.CapturedMonitorRectangle == null)
                    throw new Exception("Could not get rectangle for monitor '" + Settings.General.CapturedMonitorDeviceName + "'");
                //if (Settings.General.CapturedMonitorRectangle == null)
                //    throw new Exception("Could not get rectangle for monitor '" + Settings.General.CapturedMonitorDeviceName + "'. Properly edit and save monitor setting in the systray menu.");
            }
            WinApi.User32.RECT a = (WinApi.User32.RECT)Settings.General.CapturedMonitorRectangle;
            string source = " -offset_x " + a.Left + " -offset_y " + a.Top + " -video_size " + (a.Right - a.Left) + "x" + (a.Bottom - a.Top) + " -show_region 1 -i desktop ";

            arguments = Regex.Replace(arguments, @"-framerate\s+\d+", "$0" + source);
            commandLine = "\"" + Log.AppDir + "\\ffmpeg.exe\" " + arguments;
            
            WinApi.Advapi32.CreationFlags dwCreationFlags = 0;
            if (!Settings.General.ShowMpegWindow)
            {
                dwCreationFlags |= WinApi.Advapi32.CreationFlags.CREATE_NO_WINDOW;
                //startupInfo.dwFlags |= Win32Process.STARTF_USESTDHANDLES;
                //startupInfo.wShowWindow = Win32Process.SW_HIDE;
            }
            
            if (Settings.General.WriteMpegOutput2Log)
            {
                string file0 = Log.WorkDir + "\\ffmpeg_" + DateTime.Now.ToString("yyMMddHHmmss");
                string file = file0;
                for (int count = 1; File.Exists(file); count++)
                    file = file0 + "_" + count.ToString();
                file += ".log";

                File.WriteAllText(file, @"STARTED AT " + DateTime.Now.ToString() + @":
>" + commandLine + @"

", Encoding.UTF8);
                FileSecurity fileSecurity = File.GetAccessControl(file);
                FileSystemAccessRule fileSystemAccessRule = new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.AppendData, AccessControlType.Allow);
                fileSecurity.AddAccessRule(fileSystemAccessRule);
                File.SetAccessControl(file, fileSecurity);
                
                commandLine = Environment.SystemDirectory + "\\cmd.exe /c \"" + commandLine + " 1>>\"" + file + "\",2>&1\"";
            }

            uint processId = ProcessRoutines.CreateProcessAsUserOfParentProcess(sessionId, commandLine, dwCreationFlags);
            mpeg_stream_process = Process.GetProcessById((int)processId);
            if (mpeg_stream_process == null)
                throw new Exception("Could not find process #" + processId);
            if (mpeg_stream_process.HasExited)
                throw new Exception("Process #" + processId + " exited with code: " + mpeg_stream_process.ExitCode);
            if (antiZombieTracker != null)
                antiZombieTracker.KillTrackedProcesses();
            antiZombieTracker = new ProcessRoutines.AntiZombieTracker();
            antiZombieTracker.Track(mpeg_stream_process);
        }
        static Process mpeg_stream_process = null;
        static string commandLine = null;
        static FileStream fileStream = null;
        static ProcessRoutines.AntiZombieTracker antiZombieTracker = null;
        static string userSessionProbe = "UserSessionProbe.exe";

        public static void Stop()
        {
            if (mpeg_stream_process != null)
            {
                Log.Main.Inform("Terminating:\r\n" + commandLine);
                ProcessRoutines.KillProcessTree(mpeg_stream_process.Id);
                mpeg_stream_process = null;
            }
            if (fileStream != null)
            {
                fileStream.Flush();
                fileStream.Dispose();
                fileStream = null;
            }
            if (antiZombieTracker != null)
            {
                antiZombieTracker.KillTrackedProcesses();
                antiZombieTracker = null;
            }
            commandLine = null;
        }

        public static bool Running
        {
            get
            {
                return mpeg_stream_process != null;
            }
        }

        public static string CommandLine
        {
            get
            {
                return commandLine;
            }
        }
    }
}
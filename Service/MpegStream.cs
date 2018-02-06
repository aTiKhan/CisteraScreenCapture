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
            lock (lockObject)
            {
                MpegStream.sessionId = sessionId;

                if (sessionId < 1)
                    throw new Exception("sessionId == " + sessionId);

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
                string source;
                if (Settings.General.CapturedMonitorDeviceName == null || Settings.General.CapturedMonitorDeviceName == Settings.GeneralSettings.CapturedMonitorDeviceName_ALL_DISPLAYS)
                {
                    source = " -i desktop ";
                }
                else
                {
                    if (Settings.General.CapturedMonitorRectangle == null)
                    {
                        Log.Main.Inform("CapturedMonitorRectangle is empty. Running " + userSessionAgent);
                        UserSessionApi.OpenApi();
                        WinApi.Advapi32.CreationFlags cfs = 0;
                        cfs |= WinApi.Advapi32.CreationFlags.CREATE_NO_WINDOW;
                        string cl = "\"" + Log.AppDir + "\\" + userSessionAgent + "\"";
                        uint pid = ProcessRoutines.CreateProcessAsUserOfCurrentProcess(sessionId, cl, cfs);
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
                    source = " -offset_x " + a.Left + " -offset_y " + a.Top + " -video_size " + (a.Right - a.Left) + "x" + (a.Bottom - a.Top) + " -show_region 1 -i desktop ";
                }

                arguments = Regex.Replace(arguments, @"-framerate\s+\d+", "$0" + source);
                commandLine = "\"" + Log.AppDir + "\\ffmpeg.exe\" " + arguments;

                dwCreationFlags = 0;
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

                start();
            }
        }
        static uint sessionId;
        static WinApi.Advapi32.CreationFlags dwCreationFlags;

        static void start()
        {
            lock (lockObject)
            {
                if (mpeg_stream_process != null && !mpeg_stream_process.HasExited)
                    Log.Main.Warning("The previous MpegStream was not stopped!");
                stop();

                uint processId = ProcessRoutines.CreateProcessAsUserOfCurrentProcess(sessionId, commandLine, dwCreationFlags);
                mpeg_stream_process = Process.GetProcessById((int)processId);
                if (mpeg_stream_process == null)
                    throw new Exception("Could not find process #" + processId);
                if (mpeg_stream_process.HasExited)
                    throw new Exception("Process #" + processId + " exited with code: " + mpeg_stream_process.ExitCode);
                antiZombieGuard.KillTrackedProcesses();
                antiZombieGuard.Track(mpeg_stream_process);

                mpeg_stream_process.EnableRaisingEvents = true;
                mpeg_stream_process.Exited += delegate (object sender, EventArgs e)
                {
                    lock (lockObject)
                    {
                        if (commandLine != null && (Process)sender == mpeg_stream_process)
                        {
                            Log.Main.Warning("!!!Terminated by unknown reason:\r\n" + commandLine + "\r\n. Restarting...");
                            TcpServer.NotifyServerOnError("ffmpeg terminated by unknown reason. Restarting...");
                            start();
                        }
                    }
                };
            }
        }
        static readonly object lockObject = new object();
        static Process mpeg_stream_process = null;
        static string commandLine = null;
        static readonly ProcessRoutines.AntiZombieGuard antiZombieGuard = new ProcessRoutines.AntiZombieGuard();
        static string userSessionAgent = "UserSessionAgent.exe";

        public static void Stop()
        {
            lock (lockObject)
            {
                commandLine = null;
                sessionId = 0;
                dwCreationFlags = 0;
                stop();
            }
        }

        static void stop()
        {
            lock (lockObject)
            {
                if (mpeg_stream_process != null)
                {
                    if (!mpeg_stream_process.HasExited)
                    {
                        Log.Main.Inform("Terminating:\r\n" + commandLine);
                        ProcessRoutines.KillProcessTree(mpeg_stream_process.Id);
                    }
                    mpeg_stream_process = null;
                }
                antiZombieGuard.KillTrackedProcesses();
            }
        }
    }
}
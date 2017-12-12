using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

//installing service:
//"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe" "d:\_d\_PROJECTS\CisteraScreenCapture\Service\bin\Debug\CisteraScreenCaptureService.exe"
//"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe" /u "d:\_d\_PROJECTS\CisteraScreenCapture\Service\bin\Debug\CisteraScreenCaptureService.exe"
//sc delete "Cistera Screen Capture Service"
namespace Cliver.CisteraScreenCaptureService
{
    public static class Program
    {
        public const string SERVICE_NAME = "Cistera Screen Capture Service";

        static Program()
        {
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args)
            {
                Exception e = (Exception)args.ExceptionObject;
                Log.Main.Error(e);
            };

            Log.Initialize(Log.Mode.ONLY_LOG, Log.CompanyCommonDataDir);

            //Config.Initialize(new string[] { "General" });
            Cliver.Config.Reload();
            Cliver.Config.Save();//to have a settings file
        }

        static void Main()
        {
            try
            {                
                Log.Main.Inform("Version: " + AssemblyRoutines.GetAppVersion());
                string user = ProcessRoutines.GetProcessUserName();
                string m = "User: " + user + " (";
                if (ProcessRoutines.ProcessHasElevatedPrivileges())
                    m += "as administrator";
                Log.Main.Inform(m + ")");

#if !test
                ServiceBase.Run(new Service());
#else
                UiApi.OpenApi();
                UiApi.StatusChanged(ServiceControllerStatus.StartPending);
                ServiceControllerStatus scs = ServiceControllerStatus.Running;
                for (; ; )
                {
                    System.Threading.Thread.Sleep(10000);
                    UiApi.Message(MessageType.INFORM, "test");
                    scs = scs == ServiceControllerStatus.Running ? ServiceControllerStatus.Stopped : ServiceControllerStatus.Running;
                    UiApi.StatusChanged(scs);
                }
#endif
            }
            catch(Exception e)
            {
                Log.Main.Error(e);
            }
        }

        //[ServiceContract(Namespace = "http://Microsoft.ServiceModel.Samples", SessionMode = SessionMode.Required, CallbackContract = typeof(IClientApi))]
        //public interface IServerApi
        //{
        //    [OperationContract(IsOneWay = true)]
        //    void ReloadSetting();
        //}
        //public interface IClientApi
        //{
        //    [OperationContract(IsOneWay = true)]
        //    void ServiceStarted();
        //    [OperationContract(IsOneWay = true)]
        //    void ServiceStopped();
        //}

        //[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerCall)]
        //public class CalculatorService : IServerApi
        //{
        //    public void ReloadSetting()
        //    {
        //    }
        //}
    }
}

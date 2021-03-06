﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Cliver.CisteraScreenCaptureUI.CisteraScreenCaptureService {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="", ConfigurationName="CisteraScreenCaptureService.IUiApi", CallbackContract=typeof(Cliver.CisteraScreenCaptureUI.CisteraScreenCaptureService.IUiApiCallback), SessionMode=System.ServiceModel.SessionMode.Required)]
    public interface IUiApi {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="urn:IUiApi/Subscribe")]
        void Subscribe();
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="urn:IUiApi/Subscribe")]
        System.Threading.Tasks.Task SubscribeAsync();
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="urn:IUiApi/Unsubscribe")]
        void Unsubscribe();
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="urn:IUiApi/Unsubscribe")]
        System.Threading.Tasks.Task UnsubscribeAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="urn:IUiApi/GetSettings", ReplyAction="urn:IUiApi/GetSettingsResponse")]
        Cliver.CisteraScreenCaptureService.Settings.GeneralSettings GetSettings(bool reset);
        
        [System.ServiceModel.OperationContractAttribute(Action="urn:IUiApi/GetSettings", ReplyAction="urn:IUiApi/GetSettingsResponse")]
        System.Threading.Tasks.Task<Cliver.CisteraScreenCaptureService.Settings.GeneralSettings> GetSettingsAsync(bool reset);
        
        [System.ServiceModel.OperationContractAttribute(Action="urn:IUiApi/SaveSettings", ReplyAction="urn:IUiApi/SaveSettingsResponse")]
        void SaveSettings(Cliver.CisteraScreenCaptureService.Settings.GeneralSettings general);
        
        [System.ServiceModel.OperationContractAttribute(Action="urn:IUiApi/SaveSettings", ReplyAction="urn:IUiApi/SaveSettingsResponse")]
        System.Threading.Tasks.Task SaveSettingsAsync(Cliver.CisteraScreenCaptureService.Settings.GeneralSettings general);
        
        [System.ServiceModel.OperationContractAttribute(Action="urn:IUiApi/GetLogDir", ReplyAction="urn:IUiApi/GetLogDirResponse")]
        string GetLogDir();
        
        [System.ServiceModel.OperationContractAttribute(Action="urn:IUiApi/GetLogDir", ReplyAction="urn:IUiApi/GetLogDirResponse")]
        System.Threading.Tasks.Task<string> GetLogDirAsync();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IUiApiCallback {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="urn:IUiApi/Message")]
        void Message(Cliver.CisteraScreenCaptureService.MessageType messageType, [System.ServiceModel.MessageParameterAttribute(Name="message")] string message1);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IUiApiChannel : Cliver.CisteraScreenCaptureUI.CisteraScreenCaptureService.IUiApi, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class UiApiClient : System.ServiceModel.DuplexClientBase<Cliver.CisteraScreenCaptureUI.CisteraScreenCaptureService.IUiApi>, Cliver.CisteraScreenCaptureUI.CisteraScreenCaptureService.IUiApi {
        
        public UiApiClient(System.ServiceModel.InstanceContext callbackInstance) : 
                base(callbackInstance) {
        }
        
        public UiApiClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName) : 
                base(callbackInstance, endpointConfigurationName) {
        }
        
        public UiApiClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public UiApiClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public UiApiClient(System.ServiceModel.InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, binding, remoteAddress) {
        }
        
        public void Subscribe() {
            base.Channel.Subscribe();
        }
        
        public System.Threading.Tasks.Task SubscribeAsync() {
            return base.Channel.SubscribeAsync();
        }
        
        public void Unsubscribe() {
            base.Channel.Unsubscribe();
        }
        
        public System.Threading.Tasks.Task UnsubscribeAsync() {
            return base.Channel.UnsubscribeAsync();
        }
        
        public Cliver.CisteraScreenCaptureService.Settings.GeneralSettings GetSettings(bool reset) {
            return base.Channel.GetSettings(reset);
        }
        
        public System.Threading.Tasks.Task<Cliver.CisteraScreenCaptureService.Settings.GeneralSettings> GetSettingsAsync(bool reset) {
            return base.Channel.GetSettingsAsync(reset);
        }
        
        public void SaveSettings(Cliver.CisteraScreenCaptureService.Settings.GeneralSettings general) {
            base.Channel.SaveSettings(general);
        }
        
        public System.Threading.Tasks.Task SaveSettingsAsync(Cliver.CisteraScreenCaptureService.Settings.GeneralSettings general) {
            return base.Channel.SaveSettingsAsync(general);
        }
        
        public string GetLogDir() {
            return base.Channel.GetLogDir();
        }
        
        public System.Threading.Tasks.Task<string> GetLogDirAsync() {
            return base.Channel.GetLogDirAsync();
        }
    }
}

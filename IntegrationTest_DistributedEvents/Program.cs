using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;
using System.IO;
using System.Reflection;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;

namespace IntegrationTest_DistributedEvents
{
    class Program
    {
        private static AppDomain _serverAppDomain;
        private static ZyanConnection _connection;
        private static IEventComponentSingleton _proxySingleton;
        private static IEventComponentSingleCall _proxySingleCall;
        private static ICallbackComponentSingleton _proxyCallbackSingleton;
        private static ICallbackComponentSingleCall _proxyCallbackSingleCall;

        private static int _firedCountSingleton = 0;
        private static int _firedCountSingleCall = 0;
        private static int _registrationsSingleton = 0;
        private static int _registrationsSingleCall = 0;
        private static int _callbackCountSingleton = 0;
        private static int _callbackCountSingleCall = 0;
        
        private static void RegisterEvents()
        {
            _proxySingleton.ServerEvent += new Action<string>(_proxySingleton_ServerEvent);
            _registrationsSingleton++;
            _proxySingleCall.ServerEvent += new Action<string>(_proxySingleCall_ServerEvent);
            _registrationsSingleCall++;
        }

        private static void UnregisterEvents()
        {
            _proxySingleton.ServerEvent -= new Action<string>(_proxySingleton_ServerEvent);
            _registrationsSingleton--;
            _proxySingleCall.ServerEvent -= new Action<string>(_proxySingleCall_ServerEvent);
            _registrationsSingleCall--;
        }

        private static void _proxySingleton_ServerEvent(string obj)
        {
            _firedCountSingleton++;
        }

        private static void _proxySingleCall_ServerEvent(string obj)
        {
            _firedCountSingleCall++;
        }

        private static void CallBackSingleton(string text)
        {
            _callbackCountSingleton++;
        }

        private static void CallBackSingleCall(string text)
        {
            _callbackCountSingleCall++;
        }

        public static int Main(string[] args)
        {
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            _serverAppDomain = AppDomain.CreateDomain("Server", null, setup);
            _serverAppDomain.Load("Zyan.Communication");

            CrossAppDomainDelegate serverWork = new CrossAppDomainDelegate(() =>
            {
                EventServer server = EventServer.Instance;
            });
            _serverAppDomain.DoCallBack(serverWork);

            TcpCustomClientProtocolSetup protocol = new TcpCustomClientProtocolSetup(true);
            _connection = new ZyanConnection("tcp://localhost:8083/EventTest",protocol);
            _proxySingleton = _connection.CreateProxy<IEventComponentSingleton>();
            _proxySingleCall = _connection.CreateProxy<IEventComponentSingleCall>();
            _proxyCallbackSingleton = _connection.CreateProxy<ICallbackComponentSingleton>();
            _proxyCallbackSingleCall = _connection.CreateProxy<ICallbackComponentSingleCall>();

            int successCount = 0;

            _proxyCallbackSingleton.Out_Callback = CallBackSingleton;
            _proxyCallbackSingleCall.Out_Callback = CallBackSingleCall;

            _proxyCallbackSingleton.DoSomething();
            if (_callbackCountSingleton == 1)
                successCount++;

            _proxyCallbackSingleCall.DoSomething();
            if (_callbackCountSingleCall == 1)
                successCount++;
            
            RegisterEvents();
            if (_registrationsSingleton == _proxySingleton.Registrations)
                successCount++;
            if (_registrationsSingleCall == _proxySingleCall.Registrations)
                successCount++;
            
            _proxySingleton.TriggerEvent();
            if (_firedCountSingleton == 1)
                successCount++;
            
            _proxySingleCall.TriggerEvent();
            if (_firedCountSingleCall == 1)
                successCount++;

            UnregisterEvents();
            if (_registrationsSingleton == _proxySingleton.Registrations)
                successCount++;
            if (_registrationsSingleCall == _proxySingleCall.Registrations)
                successCount++;

            _connection.Dispose();
            EventServerLocator locator = _serverAppDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, "IntegrationTest_DistributedEvents.EventServerLocator") as EventServerLocator;
            locator.GetEventServer().Dispose();
            AppDomain.Unload(_serverAppDomain);

            if (successCount == 8)
                return 0;
            else
                return 1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Protocols.Msmq;
using Zyan.Communication.Security;

namespace IntegrationTest_DistributedEvents
{
    public interface ICallbackComponentSingleton
    {
        Action<string> Out_Callback { get; set; }

        void DoSomething();
    }

    public class CallbackComponentSingleton : ICallbackComponentSingleton
    {
        public Action<string> Out_Callback
        {
            get;
            set;
        }

        public void DoSomething()
        {
            if (Out_Callback != null)
                Out_Callback(DateTime.Now.ToString());
        }
    }

    public interface ICallbackComponentSingleCall
    {
        Action<string> Out_Callback { get; set; }

        void DoSomething();
    }

    public class CallbackComponentSingleCall : ICallbackComponentSingleCall
    {
        public Action<string> Out_Callback
        {
            get;
            set;
        }

        public void DoSomething()
        {
            if (Out_Callback != null)
                Out_Callback(DateTime.Now.ToString());
        }
    }

    public interface IEventComponentSingleton
    {
        event Action<string> ServerEvent;

        void TriggerEvent();

        int Fired { get; }

        int Registrations { get; }
    }

    public class EventComponentSingleton : IEventComponentSingleton
    {
        public event Action<string> ServerEvent;

        int _firedCount = 0;

        public void TriggerEvent()
        {
            if (ServerEvent != null)
            {
                _firedCount++;
                ServerEvent(DateTime.Now.ToString());
            }
        }

        public int Fired 
        {
            get { return _firedCount; } 
        }

        public int Registrations 
        {
            get 
            {
                if (ServerEvent != null)
                    return ServerEvent.GetInvocationList().Length;
                else
                    return 0;
            }
        }
    }

    public interface IEventComponentSingleCall
    {
        event Action<string> ServerEvent;

        void TriggerEvent();
        
        int Registrations { get; }
    }

    public class EventComponentSingleCall : IEventComponentSingleCall
    {
        public event Action<string> ServerEvent;
        
        public void TriggerEvent()
        {
            if (ServerEvent != null)
            {
                ServerEvent(DateTime.Now.ToString());
            }
        }

        public int Registrations
        {
            get
            {
                if (ServerEvent != null)
                    return ServerEvent.GetInvocationList().Length;
                else
                    return 0;
            }
        }
    }
    
    public interface IRequestResponseCallbackSingleCall
    {
        void DoRequestResponse(string text, Action<string> callback);
    }

    public class RequestResponseCallbackSingleCall : IRequestResponseCallbackSingleCall
    {
        public void DoRequestResponse(string text, Action<string> callback)
        {
            callback(text + "!");
        }
    }

    public class EventServerLocator : MarshalByRefObject
    { 
        public EventServer GetEventServer()
        {
            return EventServer.Instance;
        }
    }

    public class EventServer : MarshalByRefObject, IDisposable
    {
        private static EventServer _instance = null;

        public static EventServer Instance
        {
            get 
            {
                if (_instance == null)
                    _instance = new EventServer();

                return _instance;
            }
        }

        private ZyanComponentHost _host;

        private EventServer ()
	    {
            //TcpCustomServerProtocolSetup protocol = new TcpCustomServerProtocolSetup(8083, new NullAuthenticationProvider(), true);
            MsmqServerProtocolSetup protocol = new MsmqServerProtocolSetup(@"private$\reqchannel");
            _host = new ZyanComponentHost("EventTest", protocol);
            _host.RegisterComponent<IEventComponentSingleton, EventComponentSingleton>(ActivationType.Singleton);
            _host.RegisterComponent<IEventComponentSingleCall, EventComponentSingleCall>(ActivationType.SingleCall);
            _host.RegisterComponent<ICallbackComponentSingleton, CallbackComponentSingleton>(ActivationType.Singleton);
            _host.RegisterComponent<ICallbackComponentSingleCall, CallbackComponentSingleCall>(ActivationType.SingleCall);
            _host.RegisterComponent<IRequestResponseCallbackSingleCall, RequestResponseCallbackSingleCall>(ActivationType.SingleCall);
	    }

        public void Dispose()
        {
            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }
        }
    }
}

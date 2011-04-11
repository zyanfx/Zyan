using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;
using System.Timers;

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

    public interface ITimerTriggeredEvent
    {
        event Action<DateTime> Tick;

        void StartTimer();
        void StopTimer();
    }

    public class TimerTriggeredEvent : ITimerTriggeredEvent
    {
        private Timer _timer = null;

        public TimerTriggeredEvent()
        {
            _timer = new Timer(300);
            _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
        }



        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Tick != null)
                Tick(DateTime.Now);
        }

        public event Action<DateTime> Tick;
        
        public void StartTimer()
        {
            _timer.Start();
        }

        public void StopTimer()
        {
            _timer.Stop();
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
        private ZyanComponentHost _duplexHost;
        private ComponentCatalog _catalog;
        
        private EventServer ()
	    {
            _catalog = new ComponentCatalog();
            _catalog.RegisterComponent<IEventComponentSingleton, EventComponentSingleton>(ActivationType.Singleton);
            _catalog.RegisterComponent<IEventComponentSingleCall, EventComponentSingleCall>(ActivationType.SingleCall);
            _catalog.RegisterComponent<ICallbackComponentSingleton, CallbackComponentSingleton>(ActivationType.Singleton);
            _catalog.RegisterComponent<ICallbackComponentSingleCall, CallbackComponentSingleCall>(ActivationType.SingleCall);
            _catalog.RegisterComponent<IRequestResponseCallbackSingleCall, RequestResponseCallbackSingleCall>(ActivationType.SingleCall);
            _catalog.RegisterComponent<ITimerTriggeredEvent, TimerTriggeredEvent>(ActivationType.Singleton);
            
            TcpCustomServerProtocolSetup protocol = new TcpCustomServerProtocolSetup(8083, new NullAuthenticationProvider(), true);
            _host = new ZyanComponentHost("EventTest", protocol, _catalog);            

            TcpDuplexServerProtocolSetup protocol2 = new TcpDuplexServerProtocolSetup(8084, new NullAuthenticationProvider(), true);
            _duplexHost = new ZyanComponentHost("DuplexEventTest", protocol2, _catalog);            
	    }

        public void Dispose()
        {
            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }
            if (_duplexHost != null)
            {
                _duplexHost.Dispose();
                _duplexHost = null;
            }
        }
    }
}

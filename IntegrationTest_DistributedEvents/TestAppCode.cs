using System;
using System.Timers;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;
using Zyan.Communication.ChannelSinks.Compression;
using Zyan.Communication.Protocols.Http;
using Zyan.Communication.Delegates;

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
				return EventStub.GetHandlerCount(ServerEvent);
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
				return EventStub.GetHandlerCount(ServerEvent);
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

		private ZyanComponentHost _tcpBinaryHost;
		private ZyanComponentHost _tcpCustomHost;
		private ZyanComponentHost _tcpDuplexHost;
		private ZyanComponentHost _httpCustomHost;
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

			// Setting compression threshold to 1 byte means that all messages will be compressed.
			// This setting should not be used in production code because smaller packets will grow in size.
			// By default, Zyan only compresses messages larger than 64 kilobytes (1 << 16 bytes).
			var tcpBinaryProtocol = new TcpBinaryServerProtocolSetup(8082);
			tcpBinaryProtocol.AddServerSinkBeforeFormatter(new CompressionServerChannelSinkProvider(1, CompressionMethod.LZF));
			_tcpBinaryHost = new ZyanComponentHost("TcpBinaryEventTest", tcpBinaryProtocol, _catalog);

			var tcpCustomProtocol = new TcpCustomServerProtocolSetup(8083, new NullAuthenticationProvider(), true)
			{
 				CompressionThreshold = 1,
				CompressionMethod = CompressionMethod.DeflateStream
			};
			_tcpCustomHost = new ZyanComponentHost("TcpCustomEventTest", tcpCustomProtocol, _catalog);

			var tcpDuplexProtocol = new TcpDuplexServerProtocolSetup(8084, new NullAuthenticationProvider(), true)
			{
				CompressionThreshold = 1,
				CompressionMethod = CompressionMethod.DeflateStream
			};
			_tcpDuplexHost = new ZyanComponentHost("TcpDuplexEventTest", tcpDuplexProtocol, _catalog);

			var httpCustomProtocol = new HttpCustomServerProtocolSetup(8085, new NullAuthenticationProvider(), true)
			{
				CompressionThreshold = 1,
				CompressionMethod = CompressionMethod.LZF
			};
			_httpCustomHost = new ZyanComponentHost("HttpCustomEventTest", httpCustomProtocol, _catalog);

			// use legacy blocking events mode because we check the handlers synchronously 
			ZyanComponentHost.LegacyBlockingEvents = true;
		}

		public void Dispose()
		{
			if (_tcpBinaryHost != null)
			{
				_tcpBinaryHost.Dispose();
				_tcpBinaryHost = null;
			}

			if (_tcpCustomHost != null)
			{
				_tcpCustomHost.Dispose();
				_tcpCustomHost = null;
			}

			if (_tcpDuplexHost != null)
			{
				_tcpDuplexHost.Dispose();
				_tcpDuplexHost = null;
			}

			if (_httpCustomHost != null)
			{
				_httpCustomHost.Dispose();
				_httpCustomHost = null;
			}
		}
	}
}

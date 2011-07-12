using System;
using System.Collections.Generic;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;

namespace IntegrationTest_DistributedEvents
{
	public static class TcpDuplexTest
	{
		private static ZyanConnection _connectionDuplex;
		private static IEventComponentSingleton _proxySingletonDuplex;
		private static IEventComponentSingleCall _proxySingleCallDuplex;
		private static ICallbackComponentSingleton _proxyCallbackSingletonDuplex;
		private static ICallbackComponentSingleCall _proxyCallbackSingleCallDuplex;
		private static IRequestResponseCallbackSingleCall _proxyRequestResponseSingleCallDuplex;
		private static ITimerTriggeredEvent _proxyTimerTriggeredEvent;

		private static int _firedCountSingletonDuplex = 0;
		private static int _firedCountSingleCallDuplex = 0;
		private static int _registrationsSingletonDuplex = 0;
		private static int _registrationsSingleCallDuplex = 0;
		private static int _callbackCountSingletonDuplex = 0;
		private static int _callbackCountSingleCallDuplex = 0;
		private static int _firedTimerTriggeredEvent = 0;

		private static void RegisterEventsDuplex()
		{
			_proxySingletonDuplex.ServerEvent += new Action<string>(_proxySingletonDuplex_ServerEvent);
			_registrationsSingletonDuplex++;
			_proxySingleCallDuplex.ServerEvent += new Action<string>(_proxySingleCallDuplex_ServerEvent);
			_registrationsSingleCallDuplex++;

			_proxyTimerTriggeredEvent.Tick += new Action<DateTime>(_proxyTimerTriggeredEvent_Tick);
		}

		static void _proxyTimerTriggeredEvent_Tick(DateTime obj)
		{
			_firedTimerTriggeredEvent++;
		}

		private static void UnregisterEventsDuplex()
		{
			_proxySingletonDuplex.ServerEvent -= new Action<string>(_proxySingletonDuplex_ServerEvent);
			_registrationsSingletonDuplex--;
			_proxySingleCallDuplex.ServerEvent -= new Action<string>(_proxySingleCallDuplex_ServerEvent);
			_registrationsSingleCallDuplex--;

			//_proxyTimerTriggeredEvent.Tick += new Action<DateTime>(_proxyTimerTriggeredEvent_Tick);
		}

		private static void _proxySingletonDuplex_ServerEvent(string obj)
		{
			_firedCountSingletonDuplex++;
		}

		private static void _proxySingleCallDuplex_ServerEvent(string obj)
		{
			_firedCountSingleCallDuplex++;
		}

		private static void CallBackSingletonDuplex(string text)
		{
			_callbackCountSingletonDuplex++;
		}

		private static void CallBackSingleCallDuplex(string text)
		{
			_callbackCountSingleCallDuplex++;
		}

		public static int RunTest()
		{
			// Duplex TCP Channel
			TcpDuplexClientProtocolSetup protocol = new TcpDuplexClientProtocolSetup(true);
			_connectionDuplex = new ZyanConnection("tcpex://localhost:8084/DuplexEventTest", protocol);

			_proxySingletonDuplex = _connectionDuplex.CreateProxy<IEventComponentSingleton>();
			_proxySingleCallDuplex = _connectionDuplex.CreateProxy<IEventComponentSingleCall>();
			_proxyCallbackSingletonDuplex = _connectionDuplex.CreateProxy<ICallbackComponentSingleton>();
			_proxyCallbackSingleCallDuplex = _connectionDuplex.CreateProxy<ICallbackComponentSingleCall>();
			_proxyRequestResponseSingleCallDuplex = _connectionDuplex.CreateProxy<IRequestResponseCallbackSingleCall>();
			_proxyTimerTriggeredEvent = _connectionDuplex.CreateProxy<ITimerTriggeredEvent>();

			_proxyTimerTriggeredEvent.StartTimer();

			List<int> stepsDone = new List<int>();

			_proxyCallbackSingletonDuplex.Out_Callback = CallBackSingletonDuplex;
			_proxyCallbackSingleCallDuplex.Out_Callback = CallBackSingleCallDuplex;

			_proxyCallbackSingletonDuplex.DoSomething();
			if (_callbackCountSingletonDuplex == 1)
			{
				stepsDone.Add(1);
				Console.WriteLine("[TCP Duplex] Singleton Callback Test passed.");
			}
			_proxyCallbackSingleCallDuplex.DoSomething();
			if (_callbackCountSingleCallDuplex == 1)
			{
				stepsDone.Add(2);
				Console.WriteLine("[TCP Duplex] SingleCall Callback Test passed.");
			}

			RegisterEventsDuplex();

			if (_registrationsSingletonDuplex == _proxySingletonDuplex.Registrations)
				stepsDone.Add(3);
			if (_registrationsSingleCallDuplex == _proxySingleCallDuplex.Registrations)
				stepsDone.Add(4);

			_proxySingletonDuplex.TriggerEvent();
			if (_firedCountSingletonDuplex == 1)
			{
				stepsDone.Add(5);
				Console.WriteLine("[TCP Duplex] Singleton Event Test passed.");
			}

			_proxySingleCallDuplex.TriggerEvent();
			if (_firedCountSingleCallDuplex == 1)
			{
				stepsDone.Add(6);
				Console.WriteLine("[TCP Duplex] SingleCall Event Test passed.");
			}

			Thread.Sleep(1000);

			if (_firedTimerTriggeredEvent > 1)
			{
				stepsDone.Add(7);
				Console.WriteLine("[TCP Duplex] Timer triggered Event Test passed.");
			}

			UnregisterEventsDuplex();

			if (_registrationsSingletonDuplex == _proxySingletonDuplex.Registrations)
				stepsDone.Add(8);
			if (_registrationsSingleCallDuplex == _proxySingleCallDuplex.Registrations)
				stepsDone.Add(9);

			RequestResponseResult requestResponseResult = new RequestResponseResult("TCP Duplex");

			_proxyRequestResponseSingleCallDuplex.DoRequestResponse("Success", requestResponseResult.ReceiveResponseSingleCall);

			Thread.Sleep(1000);

			if (requestResponseResult.Count == 1)
				stepsDone.Add(10);

			_connectionDuplex.Dispose();

			if (stepsDone.Count == 10)
				return 0;
			else
				return 1;
		}
	}
}

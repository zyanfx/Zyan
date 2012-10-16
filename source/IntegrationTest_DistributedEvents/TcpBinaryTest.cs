using System;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.ChannelSinks.Compression;

namespace IntegrationTest_DistributedEvents
{
	public static class TcpBinaryTest
	{
		private static ZyanConnection _connection;
		private static IEventComponentSingleton _proxySingleton;
		private static IEventComponentSingleCall _proxySingleCall;
		private static ICallbackComponentSingleton _proxyCallbackSingleton;
		private static ICallbackComponentSingleCall _proxyCallbackSingleCall;
		private static IRequestResponseCallbackSingleCall _proxyRequestResponseSingleCall;

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

		public static int RunTest()
		{
			var protocol = new TcpBinaryClientProtocolSetup();
			protocol.AddClientSinkAfterFormatter(new CompressionClientChannelSinkProvider(1, CompressionMethod.LZF));
			_connection = new ZyanConnection("tcp://localhost:8082/TcpBinaryEventTest", protocol);

			_proxySingleton = _connection.CreateProxy<IEventComponentSingleton>();
			_proxySingleCall = _connection.CreateProxy<IEventComponentSingleCall>();
			_proxyCallbackSingleton = _connection.CreateProxy<ICallbackComponentSingleton>();
			_proxyCallbackSingleCall = _connection.CreateProxy<ICallbackComponentSingleCall>();
			_proxyRequestResponseSingleCall = _connection.CreateProxy<IRequestResponseCallbackSingleCall>();

			int successCount = 0;

			_proxyCallbackSingleton.Out_Callback = CallBackSingleton;
			_proxyCallbackSingleCall.Out_Callback = CallBackSingleCall;

			_proxyCallbackSingleton.DoSomething();
			if (_callbackCountSingleton == 1)
			{
				successCount++;
				Console.WriteLine("[TCP Binary] Singleton Callback Test passed.");
			}
			_proxyCallbackSingleCall.DoSomething();
			if (_callbackCountSingleCall == 1)
			{
				successCount++;
				Console.WriteLine("[TCP Binary] SingleCall Callback Test passed.");
			}

			RegisterEvents();
			if (_registrationsSingleton == _proxySingleton.Registrations)
				successCount++;
			if (_registrationsSingleCall == _proxySingleCall.Registrations)
				successCount++;

			_proxySingleton.TriggerEvent();
			if (_firedCountSingleton == 1)
			{
				successCount++;
				Console.WriteLine("[TCP Binary] Singleton Event Test passed.");
			}

			_proxySingleCall.TriggerEvent();
			if (_firedCountSingleCall == 1)
			{
				successCount++;
				Console.WriteLine("[TCP Binary] SingleCall Event Test passed.");
			}

			UnregisterEvents();
			if (_registrationsSingleton == _proxySingleton.Registrations)
				successCount++;
			if (_registrationsSingleCall == _proxySingleCall.Registrations)
				successCount++;

			RequestResponseResult requestResponseResult = new RequestResponseResult("TCP Binary");

			_proxyRequestResponseSingleCall.DoRequestResponse("Success", requestResponseResult.ReceiveResponseSingleCall);

			Thread.Sleep(1000);

			if (requestResponseResult.Count == 1)
				successCount++;

			_connection.Dispose();

			if (successCount == 9)
				return 0;
			else
				return 1;
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;
using System.IO;
using System.Reflection;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;
using System.Threading;

namespace IntegrationTest_DistributedEvents
{
    public class RequestResponseResult
    {
        string _testName = string.Empty;

        public RequestResponseResult(string testName)
        {
            Count = 0;
            _testName = testName;
        }

        public int Count
        { get; set; }

        public void ReceiveResponseSingleCall(string text)
        {
            Console.WriteLine(string.Format("[{1}] Request/Response: {0}", text,_testName));
            Count++;
        }
    }

    class Program
    {
        private static AppDomain _serverAppDomain;               

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


            // Test TCP Custom
            int tcpCustomTestResult = TcpCustomTest.RunTest();

            // Test TCP Duplex
            int tcpDuplexTestResult = TcpDuplexTest.RunTest();
            

            EventServerLocator locator = _serverAppDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, "IntegrationTest_DistributedEvents.EventServerLocator") as EventServerLocator;
            locator.GetEventServer().Dispose();
            AppDomain.Unload(_serverAppDomain);

            return (tcpCustomTestResult == 0 && tcpDuplexTestResult == 0) ? 0 : 1;
        }
    }
}

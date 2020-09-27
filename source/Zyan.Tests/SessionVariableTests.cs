using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Null;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;
using Zyan.Communication.SessionMgmt;

namespace Zyan.Tests
{
	#region Unit testing platform abstraction layer
#if NUNIT
	using NUnit.Framework;
	using TestClass = NUnit.Framework.TestFixtureAttribute;
	using TestMethod = NUnit.Framework.TestAttribute;
	using ClassInitializeNonStatic = NUnit.Framework.OneTimeSetUpAttribute;
	using ClassInitialize = DummyAttribute;
	using ClassCleanupNonStatic = NUnit.Framework.OneTimeTearDownAttribute;
	using ClassCleanup = DummyAttribute;
	using TestContext = System.Object;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for session variables
	/// </summary>
	[TestClass]
	public class SessionVariableTests
	{
		public interface ISessionSample
		{
			string Get(string name);
			string Get(string name, string defaultValue);
			void Set(string name, string value);
		}

		public class SessionSample : ISessionSample
		{
			static ISessionVariableAdapter V => ServerSession.CurrentSession.SessionVariables;
			public string Get(string name) => V.GetSessionVariable<string>(name);
			public string Get(string name, string defaultValue) => V.GetSessionVariable(name, defaultValue);
			public void Set(string name, string value) => V[name] = value;
		}

		[TestMethod]
		public void SessionVariablesAreStoredWithinTheCurrentSession()
		{
			var server = new NullServerProtocolSetup(123);
			var client = new NullClientProtocolSetup();

			using (var host = new ZyanComponentHost("SessionSample", server))
			{
				host.RegisterComponent<ISessionSample, SessionSample>();

				using (var conn = new ZyanConnection(client.FormatUrl(123, "SessionSample"), client))
				{
					var proxy = conn.CreateProxy<ISessionSample>();
					proxy.Set("Hello", "World");
					Assert.AreEqual("World", proxy.Get("Hello"));

					var temp = proxy.Get("Undefined");
					Assert.IsNull(temp);
					proxy.Set("Undefined", "Defined");
					Assert.AreEqual("Defined", proxy.Get("Undefined"));
				}
			}
		}
	}
}

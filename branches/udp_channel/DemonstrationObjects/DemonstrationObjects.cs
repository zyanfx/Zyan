using System;
using System.Runtime.Remoting.Messaging;

namespace DemonstrationObjects
{
	public class Demo : MarshalByRefObject
	{
		public Demo()
		{
			Console.WriteLine("Demo constructor called");
		}

		public String PrintText(String text)
		{
			Console.WriteLine("The following text is from the client: " + text);
			return "Hello from Server";
		}

		[OneWay]
		public void TestOneWay()
		{
			Console.WriteLine("OneWay Test");
		}

		public int TestAsync(int SomeValue)
		{
			Console.WriteLine("TestAsync: Value from Client: {0}", SomeValue);

			return SomeValue + 10;
		}
	}
}

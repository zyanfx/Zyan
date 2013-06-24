using System;
using System.Text;
using System.Threading;
using Zyan.Examples.Android.Shared;

namespace Zyan.Examples.Android.ConsoleServer
{
	public class SampleService : ISampleService
	{
		Random rnd = new Random();

		const string AvailableChars = "abcdefghijklmnopqrstuvwxyz0123456789";

		public string GetRandomString()
		{
			var count = 3 + rnd.Next(10);
			var sb = new StringBuilder();
			for (var i = 0; i < count; i++)
			{
				var c = AvailableChars[rnd.Next(AvailableChars.Length)];
				sb.Append(c);
			}

			if (rnd.Next(10) < 3)
			{
				ThreadPool.QueueUserWorkItem(x => OnRandomEvent(EventArgs.Empty));
			}

			Console.WriteLine("Returning: {0}", sb);
			return sb.ToString();
		}

		public event EventHandler RandomEvent;

		private void OnRandomEvent(EventArgs e)
		{
			var randomEvent = RandomEvent;
			if (randomEvent != null)
			{
				// simulate asynchronous event
				Thread.Sleep(TimeSpan.FromSeconds(1 + rnd.Next(5)));
				Console.WriteLine("Generating random event...");
				randomEvent(null, e);
			}
		}
	}
}


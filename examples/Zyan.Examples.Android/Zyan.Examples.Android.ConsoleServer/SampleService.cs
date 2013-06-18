using System;
using System.Text;
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

			Console.WriteLine("Returning: {0}", sb);
			return sb.ToString();
		}
	}

}


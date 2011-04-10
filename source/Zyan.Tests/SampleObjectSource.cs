using System.Collections.Generic;
using Zyan.InterLinq;

namespace Zyan.Tests
{
	/// <summary>
	/// Sample queryable component implementation
	/// </summary>
	public class SampleObjectSource : IObjectSource
	{
		IEnumerable<string> Strings { get; set; }

		public SampleObjectSource()
		{
			Strings = new[] { "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog" };
		}

		public SampleObjectSource(string[] strings)
		{ 
			Strings = strings;
		}

		public IEnumerable<T> Get<T>() where T : class
		{
			if (typeof(T) == typeof(string))
			{
				foreach (var s in Strings)
				{
					yield return (T)(object)s;
				}
			}
		}
	}
}

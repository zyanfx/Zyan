using System;

namespace Zyan.Examples.Android.Shared
{
	public interface ISampleService
	{
		string GetRandomString();

		event EventHandler RandomEvent;
	}
}


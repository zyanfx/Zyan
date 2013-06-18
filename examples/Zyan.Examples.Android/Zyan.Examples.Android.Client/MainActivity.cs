using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Examples.Android.Shared;

namespace Zyan.Examples.Android.Client
{
	[Activity (Label = "Zyan.Examples.Android.Client", MainLauncher = true)]
	public class Activity1 : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button> (Resource.Id.myButton);
			
			button.Click += delegate
			{
				var protocol = new TcpDuplexClientProtocolSetup(false); // true);
				using (var conn = new ZyanConnection("tcpex://192.168.254.104:12345/Sample", protocol))
				{
					var proxy = conn.CreateProxy<ISampleService>();
					button.Text = proxy.GetRandomString();
				}
			};
		}
	}
}



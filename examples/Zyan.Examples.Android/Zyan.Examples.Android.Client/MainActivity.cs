using System;
using System.Threading.Tasks;
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

			EditText serverEditText = FindViewById<EditText> (Resource.Id.serverEditText);
			Button button = FindViewById<Button> (Resource.Id.myButton);
			TextView responseView = FindViewById<TextView> (Resource.Id.responseView);

			button.Click += async (sender, e) =>
			{
				serverEditText.Enabled = false;
				ServerAddress = serverEditText.Text;
				responseView.Text = "...";
				responseView.Text = await Task.Factory.StartNew(() => SampleService.GetRandomString());
			};
		}

		private string ServerAddress { get; set; }

		private ISampleService sampleService;

		private ISampleService SampleService
		{
			get
			{
				if (sampleService == null)
				{
					sampleService = ZyanConnection.CreateProxy<ISampleService> ();
				}

				return sampleService;
			}
		}

		private ZyanConnection zyanConnection;

		private ZyanConnection ZyanConnection
		{
			get
			{
				if (zyanConnection == null)
				{
					zyanConnection = new ZyanConnection("tcpex://" + ServerAddress + ":12345/Sample");
				}

				return zyanConnection;
			}
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			if (zyanConnection != null)
			{
				zyanConnection.Dispose();
				zyanConnection = null;
			}
		}
	}
}
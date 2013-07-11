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
	/// <summary>
	/// Main activity of the application.
	/// </summary>
	[Activity(Label = "Zyan.Examples.Android", MainLauncher = true)]
	public class MainActivity : Activity
	{
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			// get the reference to our custom application 
			App = Application as ZyanApplication;

			// initialize controls
			var serverEditText = FindViewById<EditText>(Resource.Id.serverEditText);
			var responseView = FindViewById<TextView>(Resource.Id.responseView);
			var statusTextView = FindViewById<TextView>(Resource.Id.statusTextView);
			var connectButton = FindViewById<Button>(Resource.Id.connectButton);
			var disconnectButton = FindViewById<Button>(Resource.Id.disconnectButton);
			var queryButton = FindViewById<Button>(Resource.Id.myButton);

			// enable or disable buttons
			var connected = App.ZyanConnection != null;
			serverEditText.Enabled = !connected;
			connectButton.Enabled = !connected;
			disconnectButton.Enabled = connected;
			queryButton.Enabled = connected;
			if (connected)
			{
				statusTextView.Text = "Connected to: " + ZyanConnection.ServerUrl;
			}

			// add event handlers
			connectButton.Click += async (sender, e) => 
			{
				connectButton.Enabled = false;
				serverEditText.Enabled = false;
				ServerAddress = serverEditText.Text;

				statusTextView.Text = "Connecting to server...";
				var address = await RunAsync(() => ZyanConnection.ServerUrl);
				statusTextView.Text = "Connected to: " + address;
				disconnectButton.Enabled = true;
				queryButton.Enabled = true;
			};

			disconnectButton.Click += (sender, e) => 
			{
				serverEditText.Enabled = true;
				connectButton.Enabled = true;
				queryButton.Enabled = false;
				disconnectButton.Enabled = false;
				if (App.ZyanConnection != null)
				{
					App.ZyanConnection.Dispose();
					App.ZyanConnection = null;
				}
			};

			queryButton.Click += async (sender, e) =>
			{
				responseView.Text = "...";
				responseView.Text = await Task.Factory.StartNew(() => SampleService.GetRandomString());
			};
		}

		private ZyanApplication App { get; set; }

		private string ServerAddress { get; set; }

		private ZyanConnection ZyanConnection
		{
			get
			{
				if (App.ZyanConnection == null)
				{
					ShowToast("Establishing shared secure connection...");
					App.ZyanConnection = new ZyanConnection("tcpex://" + ServerAddress + ":12345/Sample");
				}

				return App.ZyanConnection;
			}
		}

		private ISampleService sampleService;

		private ISampleService SampleService
		{
			get
			{
				if (sampleService == null)
				{
					sampleService = ZyanConnection.CreateProxy<ISampleService>();

					// subscribe to remote event
					SampleService.RandomEvent += SampleService_OnRandomEvent;
					OnDestroyHandlers += () =>
					{
						// unsubscribe from remote event when OnDestroy is called to prevent the activity leak 
						SampleService.RandomEvent -= SampleService_OnRandomEvent;
					};
				}

				return sampleService;
			}
		}

		private Task<T> RunAsync<T>(Func<T> func)
		{
			return Task.Factory.StartNew(func);
		}

		private void ShowToast(string text, ToastLength length = ToastLength.Long)
		{
			RunOnUiThread(() => Toast.MakeText(this, text, length).Show());
		}

		private void SampleService_OnRandomEvent(object sender, EventArgs args)
		{
			ShowToast("Random event: " + DateTime.Now.ToString("mm:ss"), ToastLength.Short);
		}

		private Action OnDestroyHandlers { get; set; }

		protected override void OnDestroy()
		{
			base.OnDestroy();

			var handlers = OnDestroyHandlers;
			if (handlers != null)
			{
				handlers();
			}
		}
	}
}
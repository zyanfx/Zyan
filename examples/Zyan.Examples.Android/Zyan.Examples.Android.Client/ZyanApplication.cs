using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Zyan.Communication;

namespace Zyan.Examples.Android.Client
{
	/// <summary>
	/// Custom application class to manage data shared between different activities, i.e. ZyanConnection.
	/// </summary>
	[Application(Debuggable = true, Label = "Zyan.Android.Client example", ManageSpaceActivity = typeof(MainActivity))]
	public class ZyanApplication : Application
	{
		public ZyanApplication(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
		}

		/// <summary>
		/// Gets or sets the connection shared by all activities within the application.
		/// </summary>
		public ZyanConnection ZyanConnection { get; set; }

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing && ZyanConnection != null)
			{
				ZyanConnection.Dispose();
				ZyanConnection = null;
			}
		}
	}
}


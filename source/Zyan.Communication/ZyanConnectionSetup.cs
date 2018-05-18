using System;
using System.Collections;
using Zyan.Communication.Protocols;
using Zyan.Communication.Security;

namespace Zyan.Communication
{
	/// <summary>
	/// Describes configuration settings for setup a single Zyan Connection.
	/// </summary>
	[Serializable]
	public class ZyanConnectionSetup
	{
		/// <summary>
		/// Creates a new instance of the ZyanConnectionSetup class.
		/// </summary>
		public ZyanConnectionSetup()
		{
			Credentials = new Hashtable();

			AutoLoginOnExpiredSession = false;
			KeepSessionAlive = true;
		}

		/// <summary>
		/// Get or sets the server URL (e.G. "tcp://server1:46123/host1")
		/// </summary>
		public string ServerUrl { get; set; }

		/// <summary>
		/// Gets or sets the protocol setup to be used.
		/// </summary>
		public IClientProtocolSetup ProtocolSetup { get; set; }

		/// <summary>
		/// Gets or sets the login credentials.
		/// </summary>
		public AuthCredentials Credentials { get; set; }

		/// <summary>
		/// Gets or sets wether Zyan should login automatically with cached credentials after the session is expired.
		/// </summary>
		public bool AutoLoginOnExpiredSession { get; set; }

		/// <summary>
		/// Gets or sets wether the session should be kept alive.
		/// </summary>
		public bool KeepSessionAlive { get; set; }

		/// <summary>
		/// Adds a new credential.
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="value">Value</param>
		public void AddCredential(string name, string value)
		{
			if (Credentials == null)
				Credentials = new Hashtable();

			Credentials.Add(name, value);
		}
	}
}

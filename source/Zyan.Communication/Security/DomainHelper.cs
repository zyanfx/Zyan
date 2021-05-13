namespace Zyan.Communication.Security
{
	/// <summary>
	/// http://www.pinvoke.net/default.aspx/netapi32.NetGetJoinInformation
	/// </summary>
	public class DomainHelper
	{
		[System.Runtime.InteropServices.DllImport("Netapi32.dll", EntryPoint = "NetApiBufferFree")]
		private static extern uint NetApiBufferFree(System.IntPtr buffer);
		[System.Runtime.InteropServices.DllImport("Netapi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
		static extern int NetGetJoinInformation(string server, out System.IntPtr domain, out NetJoinStatus status);

		/// <summary>
		/// NetJoinStatus
		/// </summary>
		public enum NetJoinStatus
		{
			/// <summary>
			/// unknown
			/// </summary>
			NetSetupUnknownStatus = 0,
			/// <summary>
			/// unjoined
			/// </summary>
			NetSetupUnjoined,
			/// <summary>
			/// workgroup
			/// </summary>
			NetSetupWorkgroupName,
			/// <summary>
			/// domain
			/// </summary>
			NetSetupDomainName
		}

		/// <summary>
		/// Gets the name of the joined domain.		
		/// </summary>
		/// <returns></returns>
		public static string GetJoinedDomainName()
		{
			string domainName = null;
			var pDomain = System.IntPtr.Zero;
			var status = NetJoinStatus.NetSetupUnknownStatus;
			try
			{
				if ((NetGetJoinInformation(null, out pDomain, out status) == 0) && (status == NetJoinStatus.NetSetupDomainName))
				{
					domainName = System.Runtime.InteropServices.Marshal.PtrToStringAuto(pDomain);
				}
			}
			finally
			{
				if (pDomain != System.IntPtr.Zero) NetApiBufferFree(pDomain);
			}
			 return ((domainName == null) ? "." : domainName);
		}
	}
}
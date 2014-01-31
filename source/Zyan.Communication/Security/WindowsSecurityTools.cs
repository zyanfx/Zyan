using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Zyan.Communication.Security
{
	internal class WindowsSecurityTools
	{
		[DllImport("advapi32.dll")]
		[SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
		internal static extern int LogonUser(
			string lpszUsername,
			string lpszDomain,
			string lpszPassword,
			LogonType dwLogonType,
			ProviderType dwLogonProvider,
			out IntPtr phToken);

		[DllImport("kernel32.dll")]
		[SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
		internal static extern bool CloseHandle(IntPtr phToken);

		internal enum LogonType
		{
			LOGON32_LOGON_INTERACTIVE = 2,
			LOGON32_LOGON_NETWORK = 3,
			LOGON32_LOGON_BATCH = 4,
			LOGON32_LOGON_SERVICE = 5,
			LOGON32_LOGON_UNLOCK = 7,
			LOGON32_LOGON_NETWORK_CLEARTEXT = 8,
			LOGON32_LOGON_NEW_CREDENTIALS = 9,
		}

		internal enum ProviderType
		{
			LOGON32_PROVIDER_DEFAULT = 0,
			LOGON32_PROVIDER_WINNT35 = 1,
			LOGON32_PROVIDER_WINNT40 = 2,
			LOGON32_PROVIDER_WINNT50 = 3
		}
	}
}

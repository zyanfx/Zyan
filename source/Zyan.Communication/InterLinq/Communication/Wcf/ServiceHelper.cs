using System;
using System.ServiceModel;

namespace Zyan.InterLinq.Communication.Wcf
{
	/// <summary>
	/// Helper class providing different methods required by 
	/// WCF Clients / Servers.
	/// </summary>
	public static class ServiceHelper
	{
		/// <summary>
		/// Returns the default Service <see cref="EndpointAddress"/>.
		/// </summary>
		/// <returns>Returns the default Service <see cref="EndpointAddress"/>.</returns>
		public static EndpointAddress GetEndpoint() { return GetEndpoint(null, null, null); }

		/// <summary>
		/// Returns a Service <see cref="EndpointAddress"/>.
		/// </summary>
		/// <param name="serverAddress">Address of the Server.</param>
		/// <param name="serverPort">Port of the Server.</param>
		/// <param name="servicePath">Path of the WCF Service.</param>
		/// <returns>Returns a Service <see cref="EndpointAddress"/>.</returns>
		public static EndpointAddress GetEndpoint(string serverAddress, string serverPort, string servicePath)
		{
			return new EndpointAddress(GetServiceUri(serverAddress, serverPort, servicePath));
		}

		/// <summary>
		/// Returns the default Service URI as <see langword="string" />.
		/// </summary>
		/// <returns>Returns the default Service URI as <see langword="string" />.</returns>
		public static string GetServiceUri() { return GetServiceUri(null, null, null); }

		/// <summary>
		/// Returns a Service URI as <see langword="string"/>.
		/// </summary>
		/// <param name="serverAddress">Address of the Server.</param>
		/// <param name="serverPort">Port of the Server.</param>
		/// <param name="servicePath">Path of the WCF Service.</param>
		/// <returns>Returns a Service URI as <see langword="string"/>.</returns>
		public static string GetServiceUri(string serverAddress, string serverPort, string servicePath)
		{
			return string.Format("net.tcp://{0}:{1}/{2}",
								  !string.IsNullOrEmpty(serverAddress) ? serverAddress : ServiceConstants.ServerAddress,
								  !string.IsNullOrEmpty(serverPort) ? serverPort : ServiceConstants.ServerPort,
								  !string.IsNullOrEmpty(servicePath) ? servicePath : ServiceConstants.ServicePath);
		}

		/// <summary>
		/// Returns a default <see cref="NetTcpBinding"/>.
		/// </summary>
		/// <remarks>
		/// The <see cref="NetTcpBinding"/> looks like this:
		/// 
		/// <list type="list">
		///     <listheader>
		///         <term>Property</term>
		///         <description>Value</description>
		///     </listheader>
		///     <item>
		///         <term><see cref="NetTcpSecurity.Mode">Security.Mode</see></term>
		///         <description><see cref="SecurityMode.None"/></description>
		///     </item>
		///     <item>
		///         <term><see cref="NetTcpBinding.MaxBufferSize"/></term>
		///         <description><see cref="int.MaxValue"/></description>
		///     </item>
		///     <item>
		///         <term><see cref="NetTcpBinding.MaxReceivedMessageSize"/></term>
		///         <description><see cref="int.MaxValue"/></description>
		///     </item>
		///     <item>
		///         <term><see cref="System.Xml.XmlDictionaryReaderQuotas.MaxArrayLength">ReaderQuotas.MaxArrayLength</see></term>
		///         <description><see cref="int.MaxValue"/></description>
		///     </item>
		///     <item>
		///         <term><see cref="System.ServiceModel.Channels.Binding.OpenTimeout">OpenTimeout</see></term>
		///         <description>new <see cref="TimeSpan"/>( 0, 10, 0 ) = 10 Minutes</description>
		///     </item>
		///     <item>
		///         <term><see cref="System.ServiceModel.Channels.Binding.CloseTimeout">CloseTimeout</see></term>
		///         <description>new <see cref="TimeSpan"/>( 0, 10, 0 ) = 10 Minutes</description>
		///     </item>
		///     <item>
		///         <term><see cref="System.ServiceModel.Channels.Binding.SendTimeout">SendTimeout</see></term>
		///         <description>new <see cref="TimeSpan"/>( 0, 10, 0 ) = 10 Minutes</description>
		///     </item>
		/// </list>
		/// </remarks>
		/// <returns>Returns a default <see cref="NetTcpBinding"/>.</returns>
		public static NetTcpBinding GetNetTcpBinding()
		{
			NetTcpBinding netTcpBinding = new NetTcpBinding();

			netTcpBinding.Security.Mode = SecurityMode.None;

			netTcpBinding.MaxBufferSize = int.MaxValue;
			netTcpBinding.MaxReceivedMessageSize = int.MaxValue;

			netTcpBinding.ReaderQuotas.MaxArrayLength = int.MaxValue;
			netTcpBinding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
			netTcpBinding.ReaderQuotas.MaxDepth = int.MaxValue;
			netTcpBinding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;
			netTcpBinding.ReaderQuotas.MaxStringContentLength = int.MaxValue;

			netTcpBinding.OpenTimeout = new TimeSpan(0, 10, 0);
			netTcpBinding.CloseTimeout = new TimeSpan(0, 10, 0);
			netTcpBinding.SendTimeout = new TimeSpan(0, 10, 0);
			netTcpBinding.ReceiveTimeout = new TimeSpan(0, 10, 0);

			return netTcpBinding;
		}
	}
}

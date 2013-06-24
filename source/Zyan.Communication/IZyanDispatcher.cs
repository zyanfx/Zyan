using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Zyan.Communication.Notification;
using Zyan.Communication.Delegates;

namespace Zyan.Communication
{
	/// <summary>
	/// General interface for dispatch component.
	/// </summary>
	public interface IZyanDispatcher
	{
		/// <summary>
		/// Processes remote method invocation.
		/// </summary>
		/// <param name="trackingID">Key for call tracking</param>
		/// <param name="interfaceName">Name of the component interface</param>
		/// <param name="delegateCorrelationSet">Correlation set for dynamic event and delegate wiring</param>
		/// <param name="methodName">Name of the invoked method</param>
		/// <param name="genericArguments">Generic arguments of the invoked method</param>
		/// <param name="paramTypes">Parameter types</param>
		/// <param name="args">Parameter values</param>
		/// <returns>Return value</returns>
		object Invoke(Guid trackingID, string interfaceName, List<DelegateCorrelationInfo> delegateCorrelationSet, string methodName, Type[] genericArguments, Type[] paramTypes, params object[] args);

		/// <summary>
		/// Returns an array with metadata about all registered components.
		/// </summary>
		/// <returns>Array with registered component metadata</returns>
		ComponentInfo[] GetRegisteredComponents();

		/// <summary>
		/// Processes logon.
		/// </summary>
		/// <param name="sessionID">Unique session key (created on client side)</param>
		/// <param name="credentials">Logon credentials</param>
		void Logon(Guid sessionID, Hashtable credentials);

		/// <summary>
		/// Process logoff.
		/// </summary>
		/// <param name="sessionID">Unique session key</param>
		void Logoff(Guid sessionID);

		/// <summary>
		/// Returns true, if a specified Session ID is valid, otherwis false.
		/// </summary>
		/// <param name="sessionID">Session ID to check</param>
		/// <returns>Session check result</returns>
		bool ExistSession(Guid sessionID);

		/// <summary>
		/// Gets the maximum sesseion age (in minutes).
		/// </summary>
		int SessionAgeLimit
		{
			get;
		}

		/// <summary>
		/// Extends the lifetime of the current session and returs the current session age limit.
		/// </summary>
		/// <returns>Session age limit (in minutes)</returns>
		int RenewSession();

		/// <summary>
		/// Adds a handler to an event of a server component.
		/// </summary>
		/// <param name="interfaceName">Name of the server component interface</param>
		/// <param name="correlation">Correlation information</param>
		/// <param name="uniqueName">Unique name of the server component instance (May left empty, if component isn´t registered with a unique name)</param>
		void AddEventHandler(string interfaceName, DelegateCorrelationInfo correlation, string uniqueName);

		/// <summary>
		/// Removes a handler from an event of a server component.
		/// </summary>
		/// <param name="interfaceName">Name of the server component interface</param>
		/// <param name="correlation">Correlation information</param>
		/// <param name="uniqueName">Unique name of the server component instance (May left empty, if component isn´t registered with a unique name)</param>
		void RemoveEventHandler(string interfaceName, DelegateCorrelationInfo correlation, string uniqueName);

		/// <summary>
		/// Event: Occours when a heartbeat signal is received from a client.
		/// </summary>
		event EventHandler<ClientHeartbeatEventArgs> ClientHeartbeatReceived;

		/// <summary>
		/// Called from client to send a heartbeat signal.
		/// </summary>
		/// <param name="sessionID">Client´s session key</param>
		void ReceiveClientHeartbeat(Guid sessionID);
	}
}

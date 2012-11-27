using System;
using System.Collections.Generic;

namespace Zyan.Communication.SessionMgmt
{
	/// <summary>
	/// Session variable container.
	/// </summary>
	internal class SessionVariableAdapter : ISessionVariableAdapter
	{
		// Fields
		private ISessionManager _sessionManager = null;
		private Guid _sessionID = Guid.Empty;

		/// <summary>
		/// Initializes a new instance of SessionVariableAdapter.
		/// </summary>
		/// <param name="sessionManager">Session manager component.</param>
		/// <param name="sessionID">Session unique identifier.</param>
		internal SessionVariableAdapter(ISessionManager sessionManager, Guid sessionID)
		{
			_sessionManager = sessionManager;
			_sessionID = sessionID;
		}

		/// <summary>
		/// Set the new value for a session variable with the given name.
		/// </summary>
		/// <param name="name">Variable name.</param>
		/// <param name="value">Variable value.</param>
		public void SetSessionVariable(string name, object value)
		{
			_sessionManager.SetSessionVariable(_sessionID, name, value);
		}

		/// <summary>
		/// Get an untyped value of the session variable with the given name.
		/// </summary>
		/// <param name="name">Variable name</param>
		/// <returns>
		/// Variable value.
		/// </returns>
		public object GetSessionVariable(string name)
		{
			return _sessionManager.GetSessionVariable(_sessionID, name);
		}

		/// <summary>
		/// Get strongly-typed value of the session variable with the given name.
		/// </summary>
		/// <typeparam name="T">Variable type.</typeparam>
		/// <param name="name">Variable name.</param>
		/// <returns>
		/// Variable value.
		/// </returns>
		public T GetSessionVariable<T>(string name)
		{
			var isValueType = typeof(ValueType).IsAssignableFrom(typeof(T));
			var defaultValue = isValueType ? Activator.CreateInstance<T>() : (T)(object)null;
			return GetSessionVariable(name, defaultValue);
		}

		/// <summary>
		/// Get strongly-typed value of the session variable with the given name.
		/// </summary>
		/// <typeparam name="T">Variable type.</typeparam>
		/// <param name="name">Variable name.</param>
		/// <param name="defaultValue">Default value to return if the variable is not defined.</param>
		/// <returns>
		/// Variable value.
		/// </returns>
		public T GetSessionVariable<T>(string name, T defaultValue)
		{
			var value = GetSessionVariable(name) ?? (object)defaultValue;
			return (T)(object)value;
		}

		/// <summary>
		/// Gets or sets the variable value.
		/// </summary>
		/// <param name="variableName">Variable name.</param>
		/// <returns>The value of the session variable.</returns>
		public object this[string variableName]
		{
			get { return GetSessionVariable(variableName); }
			set { SetSessionVariable(variableName, value); }
		}
	}
}

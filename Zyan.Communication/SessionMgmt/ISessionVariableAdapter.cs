using System;
using System.Collections.Generic;

namespace Zyan.Communication.SessionMgmt
{
	/// <summary>
	/// Interface for the session variable collection indexed by variable names.
	/// </summary>
	public interface ISessionVariableAdapter
	{
		/// <summary>
		/// Set the new value for a session variable with the given name.
		/// </summary>
		/// <param name="name">Variable name.</param>
		/// <param name="value">Variable value.</param>
		void SetSessionVariable(string name, object value);

		/// <summary>
		/// Get an untyped value of the session variable with the given name.
		/// </summary>
		/// <param name="name">Variable name</param>
		/// <returns>Variable value.</returns>
		object GetSessionVariable(string name);

		/// <summary>
		/// Get strongly-typed value of the session variable with the given name.
		/// </summary>
		/// <typeparam name="T">Variable type.</typeparam>
		/// <param name="name">Variable name.</param>
		/// <returns>Variable value.</returns>
		T GetSessionVariable<T>(string name);

		/// <summary>
		/// Get strongly-typed value of the session variable with the given name.
		/// </summary>
		/// <typeparam name="T">Variable type.</typeparam>
		/// <param name="name">Variable name.</param>
		/// <param name="defaultValue">Default value to return if the variable is not defined.</param>
		/// <returns>Variable value.</returns>
		T GetSessionVariable<T>(string name, T defaultValue);

		/// <summary>
		/// Gets or sets the variable value.
		/// </summary>
		/// <param name="variableName">Variable name.</param>
		/// <returns>The value of the session variable.</returns>
		object this[string variableName] { get; set; }
	}
}

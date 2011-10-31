using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication
{
	/// <summary>
	/// Beschreibt eine konkretee Aufrufabfangaktion.
	/// </summary>
	public class CallInterceptionData
	{
		// Delegat für den Aufruf von InvokeRemoteMethod (bei Bedarf)
		InvokeRemoteMethodDelegate _remoteInvoker = null;

		// Remoting-Nachricht
		IMethodCallMessage _remotingMessage = null;

		/// <summary>
		/// Erstellt eine neue Instanz der CallInterceptionData-Klasse.
		/// </summary>
		/// <param name="parameters">Parameterwerte des abgefangenen Aufrufs</param>
		/// <param name="remoteInvoker">Delegat für den Aufruf von InvokeRemoteMethod (bei Bedarf)</param>
		/// <param name="remotingMessage">Remoting-nachricht</param>
		public CallInterceptionData(object[] parameters, InvokeRemoteMethodDelegate remoteInvoker, IMethodCallMessage remotingMessage)
		{
			// Felder füllen
			Intercepted = false;
			ReturnValue = null;
			Parameters = parameters;
			_remoteInvoker = remoteInvoker;
			_remotingMessage = remotingMessage;
		}

		/// <summary>
		/// Führt den entfernten Methodenaufruf aus.
		/// </summary>
		/// <returns>Rückgabewert</returns>
		public object MakeRemoteCall()
		{
			// Entfernte Methode aufrufen
			IMessage result = _remoteInvoker(_remotingMessage, false);

			// Rückgabenachricht casten
			ReturnMessage returnMessage = result as ReturnMessage;

			// Wenn eine gültige Rückgabenachricht zurückgegeben wurde ...
			if (returnMessage != null)
				// Rückgabewert zurückgeben
				return returnMessage.ReturnValue;

			// null zurückgeben
			return null;
		}

		/// <summary>
		/// Gibt zurück, ob der Aufruf abgefangen wurde, oder legt dies fest.
		/// </summary>
		public bool Intercepted
		{
			get;
			set;
		}

		/// <summary>
		/// Gibt den zu verwendenden Rückgabewert zurück, oder legt ihn fest.
		/// </summary>
		public object ReturnValue
		{
			get;
			set;
		}

		/// <summary>
		/// Gibt ein Array der Parameterwerten zurück, mit welchen die abzufangende Methode aufgerufen wurde, oder legt sie fest.
		/// </summary>
		public object[] Parameters
		{
			get;
			set;
		}
	}
}
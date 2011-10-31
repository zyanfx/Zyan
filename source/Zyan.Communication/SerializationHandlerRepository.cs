using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zyan.Communication
{
	/// <summary>
	/// Verwaltet Serialisierungshandler.
	/// </summary>
	public class SerializationHandlerRepository : IEnumerable<ISerializationHandler>
	{
		// Auflistung der registrierten Serialisierungshandler
		private volatile Dictionary<Type, ISerializationHandler> _serializationHandlers = null;

		// Sperrobjekt zur Synchronisierung des threadübergreifenden Zugriffs auf die registrierten Serialisierungshandler
		private object _serializationHandlersLockObject = new object();

		/// <summary>
		/// Erzeugt eine neue Instanz der SerializationHandlerRepository-Klasse.
		/// </summary>
		public SerializationHandlerRepository()
		{
			// Auflistung erzeugen
			_serializationHandlers = new Dictionary<Type, ISerializationHandler>();
		}

		/// <summary>
		/// Registriert einen Serialisierungshandler für einen bestimmten Typ.
		/// </summary>
		/// <param name="handledType">Typ, dessen Serialisierung behandelt werden soll</param>
		/// <param name="handler">Serialisierungshandler</param>
		public void RegisterSerializationHandler(Type handledType, ISerializationHandler handler)
		{
			// Wenn kein Typ angegebe wurde ...
			if (handledType == null)
				// Ausnahme werfen
				throw new ArgumentNullException("handledType");

			// Wenn kein Handler angegeben wurde ..
			if (handler == null)
				// Ausnahme werfen
				throw new ArgumentNullException("handler");

			lock (_serializationHandlersLockObject)
			{
				// Wenn bereits ein Handler für diesen Typ registriert ist ...
				if (_serializationHandlers.ContainsKey(handledType))
					// Ausnahme werfen
					throw new ArgumentException(string.Format(LanguageResource.ArgumentException_TypeHasAlreadyAHandler, handledType.FullName));

				// Serialisierungshandler registrieren
				_serializationHandlers.Add(handledType, handler);
			}
		}

		/// <summary>
		/// Entfernt die Registrierung eines Serialisierungshandlers für einen bestimmten Typ.
		/// </summary>
		/// <param name="handledType">Typ</param>
		public void UnregisterSerializationHandler(Type handledType)
		{
			// Wenn kein Typ angegebe wurde ...
			if (handledType == null)
				// Ausnahme werfen
				throw new ArgumentNullException("handledType");

			lock (_serializationHandlersLockObject)
			{
				// Wenn ein Serialisierungshandler für den angegebenen Typ registriert ist ...
				if (_serializationHandlers.ContainsKey(handledType))
				{
					// Registrierung aufheben
					_serializationHandlers.Remove(handledType);
				}
			}
		}

		/// <summary>
		/// Gibt den registrierten Serialisierungshandler für einen bestimmten Typ zurück.
		/// </summary>
		/// <param name="handledType">Behandelter Typ</param>
		/// <returns>Serialisierungshandler</returns>
		public ISerializationHandler this[Type handledType]
		{
			get
			{
				// Wenn kein Typ angegebe wurde ...
				if (handledType == null)
					// Ausnahme werfen
					throw new ArgumentNullException("handledType");

				lock (_serializationHandlersLockObject)
				{
					// Wenn ein Serialisierungshandler für den angegebenen Typ registriert ist ...
					if (_serializationHandlers.ContainsKey(handledType))
						// Serialisierungshandler zurückgeben
						return _serializationHandlers[handledType];
				}
				// Nicht zurückgeben
				return null;
			}
		}

		/// <summary>
		/// Ermittelt - wenn möglich - einen passenden Serialisierungshandler für den angegebenen Typen.
		/// </summary>
		/// <param name="type">Typ</param>
		/// <param name="handledType">Behandelter Typ</param>
		/// <param name="handler">Serialisierungshandler</param>
		public void FindMatchingSerializationHandler(Type type, out Type handledType, out ISerializationHandler handler)
		{
			// Wenn kein Typ angegebe wurde ...
			if (type == null)
				// Ausnahme werfen
				throw new ArgumentNullException("type");

			// Standardmäßig nicht szurückgeben
			handledType = null;
			handler = null;

			lock (_serializationHandlersLockObject)
			{
				// Ersten passenden Serialisierungshandlertyp suchen
				handledType = (from handleType in _serializationHandlers.Keys
							   where handleType.IsAssignableFrom(type)
							   select handleType).FirstOrDefault();

				// Wenn ein passender Typ gefunden wurde ...
				if (handledType != null)
					// Passenden Serialisierungshandler abrufen
					handler = _serializationHandlers[handledType];
			}
		}

		/// <summary>
		/// Gibt einen typisierten Enumerator zurück.
		/// </summary>
		/// <returns>Typisierter Enumerator</returns>
		public IEnumerator<ISerializationHandler> GetEnumerator()
		{
			// Enumerator erzeugen und zurückgeben
			return _serializationHandlers.Values.GetEnumerator();
		}

		/// <summary>
		/// Gibt einen Enumerator zurück.
		/// </summary>
		/// <returns>Enumerator</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			// Enumerator erzeugen und zurückgeben
			return _serializationHandlers.Values.GetEnumerator();
		}
	}
}

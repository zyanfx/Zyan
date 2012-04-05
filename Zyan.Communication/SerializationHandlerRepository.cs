using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zyan.Communication
{
	/// <summary>
	/// Repository of serialization handlers.
	/// </summary>
	public class SerializationHandlerRepository : IEnumerable<ISerializationHandler>
	{
		// Dictionary of registered serialization handlers
		private volatile Dictionary<Type, ISerializationHandler> _serializationHandlers = null;

		// Lock object for thread synchronization
		private object _serializationHandlersLockObject = new object();

		/// <summary>
		/// Creates a new instance of the SerializationHandlerRepository class.
		/// </summary>
		public SerializationHandlerRepository()
		{
			_serializationHandlers = new Dictionary<Type, ISerializationHandler>();
		}

		/// <summary>
		/// Registers a serialization handler for a specified type.
		/// </summary>
		/// <param name="handledType">Type</param>
		/// <param name="handler">Serialization handler</param>
		public void RegisterSerializationHandler(Type handledType, ISerializationHandler handler)
		{
			if (handledType == null)
				throw new ArgumentNullException("handledType");

			if (handler == null)
				throw new ArgumentNullException("handler");

			lock (_serializationHandlersLockObject)
			{
				if (_serializationHandlers.ContainsKey(handledType))
					throw new ArgumentException(string.Format(LanguageResource.ArgumentException_TypeHasAlreadyAHandler, handledType.FullName));

				_serializationHandlers.Add(handledType, handler);
			}
		}

		/// <summary>
		/// Removes custom serialization handling for a specified type.
		/// </summary>
		/// <param name="handledType">Type</param>
		public void UnregisterSerializationHandler(Type handledType)
		{
			if (handledType == null)
				throw new ArgumentNullException("handledType");

			lock (_serializationHandlersLockObject)
			{
				if (_serializationHandlers.ContainsKey(handledType))
					_serializationHandlers.Remove(handledType);
			}
		}

		/// <summary>
		/// Returns a registered serialization handler for a specified type.
		/// </summary>
		/// <param name="handledType">Type</param>
		/// <returns>Serialization handler</returns>
		public ISerializationHandler this[Type handledType]
		{
			get
			{
				if (handledType == null)
					throw new ArgumentNullException("handledType");

				lock (_serializationHandlersLockObject)
				{
					if (_serializationHandlers.ContainsKey(handledType))
						return _serializationHandlers[handledType];
				}
				return null;
			}
		}

		/// <summary>
		/// Find a matching serialization handler for a specified type.
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="handledType">Originally handled type</param>
		/// <param name="handler">Serialization handler</param>
		public void FindMatchingSerializationHandler(Type type, out Type handledType, out ISerializationHandler handler)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			handledType = null;
			handler = null;

			lock (_serializationHandlersLockObject)
			{
				handledType = (from handleType in _serializationHandlers.Keys
							   where handleType.IsAssignableFrom(type)
							   select handleType).FirstOrDefault();

				if (handledType != null)
					handler = _serializationHandlers[handledType];
			}
		}

		/// <summary>
		/// Returns a typed enumerator.
		/// </summary>
		/// <returns>Enumerator</returns>
		public IEnumerator<ISerializationHandler> GetEnumerator()
		{
			return _serializationHandlers.Values.GetEnumerator();
		}

		/// <summary>
		/// Returns a untyped enumerator.
		/// </summary>
		/// <returns>Enumerator</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _serializationHandlers.Values.GetEnumerator();
		}
	}
}

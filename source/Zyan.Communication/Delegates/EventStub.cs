using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Event stub caches all event handlers for a single-call component.
	/// </summary>
	public class EventStub
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EventStub" /> class.
		/// </summary>
		/// <param name="interfaceType">Type of the interface.</param>
		public EventStub(Type interfaceType)
		{
			if (interfaceType == null)
			{
				throw new ArgumentNullException("interfaceType");
			}

			InterfaceType = interfaceType;
			CreateDelegateHolders();
		}

		/// <summary>
		/// Gets or sets the invocation delegates for event handlers.
		/// </summary>
		private Dictionary<string, IDelegateHolder> DelegateHolders { get; set; }

		/// <summary>
		/// Gets the type of the interface.
		/// </summary>
		public Type InterfaceType { get; private set; }

		/// <summary>
		/// Gets or sets the <see cref="Delegate" /> with the specified event property name.
		/// </summary>
		/// <param name="propertyName">Name of the event or delegate property.</param>
		public Delegate this[string propertyName]
		{
			get { return DelegateHolders[propertyName].InvocationDelegate; }
		}

		/// <summary>
		/// Gets or sets the list of event of the reflected interface.
		/// </summary>
		private EventInfo[] EventProperties { get; set; }

		/// <summary>
		/// Gets or sets the list of delegate properties of the reflected interface.
		/// </summary>
		private PropertyInfo[] DelegateProperties { get; set; }

		private void CreateDelegateHolders()
		{
			var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			DelegateHolders = new Dictionary<string, IDelegateHolder>();
			EventProperties = InterfaceType.GetEvents(bindingFlags);
			DelegateProperties = InterfaceType.GetProperties(bindingFlags)
				.Where(p => typeof(Delegate).IsAssignableFrom(p.PropertyType))
				.ToArray();

			foreach (var eventProperty in EventProperties)
			{
				DelegateHolders[eventProperty.Name] = CreateDelegateHolder(eventProperty.EventHandlerType);
			}

			foreach (var delegateProperty in DelegateProperties)
			{
				DelegateHolders[delegateProperty.Name] = CreateDelegateHolder(delegateProperty.PropertyType);
			}
		}

		private IDelegateHolder CreateDelegateHolder(Type delegateType)
		{
			var method = GetType().GetMethod("CreateGenericDelegateHolder", BindingFlags.Instance | BindingFlags.NonPublic);
			method = method.MakeGenericMethod(delegateType);
			return (IDelegateHolder)method.Invoke(this, null);
		}

		private IDelegateHolder CreateGenericDelegateHolder<T>()
		{
			return new DelegateHolder<T>();
		}

		/// <summary>
		/// Non-generic interface for a private delegate holder class.
		/// </summary>
		private interface IDelegateHolder
		{
			Delegate InvocationDelegate { get; }

			void AddHandler(Delegate handler);

			void RemoveHandler(Delegate handler);
		}

		/// <summary>
		/// Generic holder for delegates (such as event handlers).
		/// </summary>
		private class DelegateHolder<T> : IDelegateHolder
		{
			public Delegate InvocationDelegate
			{
				get { return (Delegate)(object)InvocationMethod; }
			}

			private T invocationMethod;

			public T InvocationMethod
			{
				get
				{
					if (invocationMethod == null)
					{
						// get Invoke method
						var dynamicInvokeMethod = GetType().GetMethod("DynamicInvoke", BindingFlags.NonPublic | BindingFlags.Instance);
						invocationMethod = DynamicWireFactory.BuildDelegate<T>(dynamicInvokeMethod, this);
					}

					return invocationMethod;
				}
			}

			private object DynamicInvoke(object[] arguments)
			{
				if (Delegate != null)
				{
					return Delegate.DynamicInvoke(arguments);
				}

				return null;
			}

			private T TypedDelegate { get; set; }

			private Delegate Delegate
			{
				get { return ((Delegate)(object)TypedDelegate); }
				set { TypedDelegate = (T)(object)value; }
			}

			public void AddHandler(Delegate handler)
			{
				Delegate = Delegate.Combine(Delegate, handler);
			}

			public void RemoveHandler(Delegate handler)
			{
				Delegate = Delegate.Remove(Delegate, handler);
			}
		}

		/// <summary>
		/// Wires all event handlers to the specified instance.
		/// </summary>
		/// <param name="instance">The instance.</param>
		public void WireTo(object instance)
		{
			if (instance == null)
			{
				return;
			}

			foreach (var eventInfo in EventProperties)
			{
				eventInfo.AddEventHandler(instance, this[eventInfo.Name]);
			}

			var indexes = new object[0];

			foreach (var propInfo in DelegateProperties)
			{
				var value = propInfo.GetValue(instance, indexes) as Delegate;
				value = Delegate.Combine(value, this[propInfo.Name]);
				propInfo.SetValue(instance, value, indexes);
			}
		}

		/// <summary>
		/// Unwires all event handlers from the specified instance.
		/// </summary>
		/// <param name="instance">The instance.</param>
		public void UnwireFrom(object instance)
		{
			if (instance == null)
			{
				return;
			}

			foreach (var eventInfo in EventProperties)
			{
				eventInfo.RemoveEventHandler(instance, this[eventInfo.Name]);
			}

			var indexes = new object[0];

			foreach (var propInfo in DelegateProperties)
			{
				var value = propInfo.GetValue(instance, indexes) as Delegate;
				value = Delegate.Remove(value, this[propInfo.Name]);
				propInfo.SetValue(instance, value, indexes);
			}
		}

		/// <summary>
		/// Adds the handler for the given event.
		/// </summary>
		/// <param name="name">The name of the event or delegate property.</param>
		/// <param name="handler">The handler.</param>
		public void AddHandler(string name, Delegate handler)
		{
			DelegateHolders[name].AddHandler(handler);
		}

		/// <summary>
		/// Removes the handler for the given event.
		/// </summary>
		/// <param name="name">The name of the event or delegate property.</param>
		/// <param name="handler">The handler.</param>
		public void RemoveHandler(string name, Delegate handler)
		{
			DelegateHolders[name].RemoveHandler(handler);
		}
	}
}

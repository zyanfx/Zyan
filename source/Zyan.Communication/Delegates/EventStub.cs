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
			EventProperties = GetEvents(InterfaceType, bindingFlags).ToArray();
			DelegateProperties = GetDelegateProperties(InterfaceType, bindingFlags).ToArray();

			foreach (var eventProperty in EventProperties)
			{
				DelegateHolders[eventProperty.Name] = CreateDelegateHolder(eventProperty.EventHandlerType);
			}

			foreach (var delegateProperty in DelegateProperties)
			{
				DelegateHolders[delegateProperty.Name] = CreateDelegateHolder(delegateProperty.PropertyType);
			}
		}

		private IEnumerable<Type> GetAllInterfaces(Type interfaceType)
		{
			if (interfaceType.IsInterface)
			{
				yield return interfaceType;
			}

			// Passing BindingFlags.FlattenHierarchy to one of the Type.GetXXX methods, such as Type.GetMembers,
			// will not return inherited interface members when you are querying on an interface type itself.
			// To get the inherited members, you need to query each implemented interface for its members.
			var inheritedInterfaces =
				from inheritedInterface in interfaceType.GetInterfaces()
				from type in GetAllInterfaces(inheritedInterface)
				select type;

			foreach (var type in inheritedInterfaces)
			{
				yield return type;
			}
		}

		private IEnumerable<EventInfo> GetEvents(Type interfaceType, BindingFlags flags)
		{
			return
				from type in GetAllInterfaces(interfaceType)
				from ev in type.GetEvents(flags)
				select ev;
		}

		private IEnumerable<PropertyInfo> GetDelegateProperties(Type interfaceType, BindingFlags flags)
		{
			return
				from type in GetAllInterfaces(interfaceType)
				from prop in type.GetProperties(flags)
				where typeof(Delegate).IsAssignableFrom(prop.PropertyType)
				select prop;
		}

		private static IDelegateHolder CreateDelegateHolder(Type delegateType)
		{
			var createDelegateHolder = createDelegateHolderMethod.MakeGenericMethod(delegateType).CreateDelegate<Func<IDelegateHolder>>();
			return createDelegateHolder();
		}

		private static MethodInfo createDelegateHolderMethod = new Func<IDelegateHolder>(CreateDelegateHolder<Action>).Method.GetGenericMethodDefinition();

		private static IDelegateHolder CreateDelegateHolder<T>()
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

			int HandlerCount { get; }
		}

		/// <summary>
		/// Generic holder for delegates (such as event handlers).
		/// </summary>
		private class DelegateHolder<T> : IDelegateHolder
		{
			public DelegateHolder()
			{
				// create default return value for the delegate
				DefaultReturnValue = typeof(T).GetMethod("Invoke").ReturnType.GetDefaultValue();
			}

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
						invocationMethod = DynamicWireFactory.BuildInstanceDelegate<T>(dynamicInvokeMethod, this);
					}

					return invocationMethod;
				}
			}

			private object DynamicInvoke(object[] arguments)
			{
				// run in legacy blocking mode
				if (ZyanSettings.LegacyBlockingEvents)
				{
					return Delegate.SafeDynamicInvoke(arguments) ?? DefaultReturnValue;
				}

				// run in non-blocking mode
				Delegate.OneWayDynamicInvoke(arguments);
				return DefaultReturnValue;
			}

			private T TypedDelegate { get; set; }

			private object DefaultReturnValue { get; set; }

			private Delegate Delegate
			{
				get { return ((Delegate)(object)TypedDelegate); }
				set { TypedDelegate = (T)(object)value; }
			}

			private object syncRoot = new object();

			public void AddHandler(Delegate handler)
			{
				lock (syncRoot)
				{
					Delegate = Delegate.Combine(Delegate, handler);
				}
			}

			public void RemoveHandler(Delegate handler)
			{
				lock (syncRoot)
				{
					Delegate = Delegate.Remove(Delegate, handler);
				}
			}

			public int HandlerCount
			{
				get
				{
					if (Delegate == null)
					{
						return 0;
					}

					return Delegate.GetInvocationList().Length;
				}
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

		/// <summary>
		/// Gets the count of event handlers for the given event or delegate property.
		/// </summary>
		/// <param name="handler">The event handler.</param>
		public static int GetHandlerCount(Delegate handler)
		{
			if (handler == null)
			{
				return 0;
			}

			var count = 0;
			foreach (var d in handler.GetInvocationList())
			{
				// check if it's a delegate holder
				if (d.Target is IDelegateHolder)
				{
					var holder = (IDelegateHolder)d.Target;
					count += holder.HandlerCount;
					continue;
				}

				// it's an ordinary subscriber
				count++;
			}

			return count;
		}
	}
}

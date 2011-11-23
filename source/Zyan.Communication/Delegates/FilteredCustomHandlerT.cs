using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Represents filtered event handler of a custom delegate type.
	/// </summary>
	/// <typeparam name="TDelegate">The type of the event handler delegate.</typeparam>
	internal class FilteredCustomHandler<TDelegate> : IFilteredEventHandler where TDelegate : class
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredCustomHandler&lt;TDelegate&gt;"/> class.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public FilteredCustomHandler(TDelegate eventHandler, IEventFilter eventFilter, bool filterLocally)
		{
			if (!(eventHandler is Delegate))
			{
				throw new ArgumentOutOfRangeException("eventHandler");
			}

			IEventFilter sourceFilter;
			ExtractSourceHandler(eventHandler, out eventHandler, out sourceFilter);

			EventHandler = eventHandler;
			EventFilter = eventFilter.Combine(sourceFilter);

			// create strong-typed invoke method
			TypedInvoke = BuildDynamicInvoke(InvokeMethodInfo);
			FilterLocally = filterLocally;
		}

		private TDelegate BuildDynamicInvoke(MethodInfo invokeMethodInfo)
		{
			// reflect delegate type to get parameters and method return type
			var delegateType = typeof(TDelegate);
			var invokeMethod = delegateType.GetMethod("Invoke");

			// the first argument is 'this' instance
			var paramTypes = Enumerable.Repeat(GetType(), 1).Concat(invokeMethod.GetParameters().Select(p => p.ParameterType)).ToArray();
			var typedInvoke = new DynamicMethod("TypedInvoke", invokeMethod.ReturnType, paramTypes, typeof(FilteredCustomHandler<TDelegate>));
			var paramCount = paramTypes.Length - 1;

			// create method body, declare local variable of type object[]
			var ilGenerator = typedInvoke.GetILGenerator();
			var argumentsArray = ilGenerator.DeclareLocal(typeof(object[]));

			// var args = new object[paramCount];
			ilGenerator.Emit(OpCodes.Nop);
			ilGenerator.Emit(OpCodes.Ldc_I4, paramCount);
			ilGenerator.Emit(OpCodes.Newarr, typeof(object));
			ilGenerator.Emit(OpCodes.Stloc, argumentsArray);

			// load method arguments one by one
			var index = 1;
			foreach (var paramType in paramTypes.Skip(1))
			{
				// load object[] array reference
				ilGenerator.Emit(OpCodes.Ldloc, argumentsArray);
				ilGenerator.Emit(OpCodes.Ldc_I4, index - 1); // array index
				ilGenerator.Emit(OpCodes.Ldarg, index++); // method parameter index

				// value type parameters need boxing
				if (typeof(ValueType).IsAssignableFrom(paramType))
				{
					ilGenerator.Emit(OpCodes.Box, paramType);
				}

				// store reference
				ilGenerator.Emit(OpCodes.Stelem_Ref);
			}

			// this
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldloc, argumentsArray); // object[] args
			ilGenerator.Emit(OpCodes.Call, invokeMethodInfo);

			// discard return value
			if (invokeMethod.ReturnType == typeof(void))
			{
				ilGenerator.Emit(OpCodes.Pop);
			}

			// unbox return value of value type
			else if (typeof(ValueType).IsAssignableFrom(invokeMethod.ReturnType))
			{
				ilGenerator.Emit(OpCodes.Unbox_Any, invokeMethod.ReturnType);
			}

			// return value
			ilGenerator.Emit(OpCodes.Ret);

			// bake dynamic method, create a gelegate
			var result = typedInvoke.CreateDelegate(delegateType, this);
			return (TDelegate)(object)result;
		}

		private void ExtractSourceHandler(TDelegate eventHandler, out TDelegate sourceHandler, out IEventFilter sourceFilter)
		{
			sourceHandler = eventHandler;
			sourceFilter = default(IEventFilter);
			var sourceDelegate = (Delegate)(object)sourceHandler;

			while (sourceDelegate.Target is IFilteredEventHandler)
			{
				var filtered = sourceDelegate.Target as IFilteredEventHandler;
				sourceHandler = filtered.EventHandler as TDelegate;
				sourceFilter = filtered.EventFilter.Combine(sourceFilter);
				sourceDelegate = (Delegate)(object)sourceHandler;
			}
		}

		/// <summary>
		/// Untyped Invoke() method.
		/// </summary>
		private object Invoke(params object[] args)
		{
			if (FilterLocally)
			{
				// filter event at the client-side, if invoked locally
				if (EventFilter != null && !EventFilter.AllowInvocation(args))
				{
					return null;
				}
			}

			// invoke client handler
			var eventHandler = (this as IFilteredEventHandler).EventHandler;
			if (eventHandler != null)
			{
				return eventHandler.DynamicInvoke(args);
			}

			return null;
		}

		/// <summary>
		/// Strong-typed Invoke() method, built dynamically.
		/// Calls the untyped Invoke() method to do the real job.
		/// </summary>
		private TDelegate TypedInvoke { get; set; }

		/// <summary>
		/// MethodInfo for the private Invoke(args) method.
		/// </summary>
		private static MethodInfo InvokeMethodInfo = typeof(FilteredCustomHandler<TDelegate>).GetMethod("Invoke",
			BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(object[]) }, null);

		/// <summary>
		/// Gets the event handler.
		/// </summary>
		public TDelegate EventHandler { get; private set; }

		/// <summary>
		/// Gets the event filter.
		/// </summary>
		public IEventFilter EventFilter { get; private set; }

		/// <summary>
		/// Performs an implicit conversion from <see cref="Zyan.Communication.Delegates.FilteredCustomHandler&lt;TEventFilter&gt;"/>
		/// to <see cref="System.EventHandler"/>.
		/// </summary>
		/// <param name="filteredEventHandler">The filtered event handler.</param>
		/// <returns>
		/// The result of the conversion.
		/// </returns>
		public static implicit operator TDelegate(FilteredCustomHandler<TDelegate> filteredEventHandler)
		{
			return filteredEventHandler.TypedInvoke;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this event filter should also work locally.
		/// </summary>
		public bool FilterLocally { get; set; }

		Delegate IFilteredEventHandler.EventHandler
		{
			get { return (Delegate)(object)EventHandler; }
		}

		IEventFilter IFilteredEventHandler.EventFilter
		{
			get { return EventFilter; }
		}
	}
}

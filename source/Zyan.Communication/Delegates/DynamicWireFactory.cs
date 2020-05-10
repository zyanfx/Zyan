using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Delegates
{
	// Factory method for dynamic wires.
	using DynamicWireFactoryMethod = Func<bool, DynamicWireBase>;
	using DynamicWireFactoryCache = ConcurrentDictionary<string, Func<bool, DynamicWireBase>>;

	/// <summary>
	/// Factory class for creation of dynamic wires.
	/// </summary>
	internal sealed class DynamicWireFactory
	{
		#region Singleton implementation

		/// <summary>
		/// This constructor is private, so DynamicWireFactory class cannot be created from the outside.
		/// </summary>
		private DynamicWireFactory()
		{
		}

		/// <summary>
		/// Lazy-initialized singleton instance.
		/// </summary>
		private static readonly Lazy<DynamicWireFactory> Instance =
			new Lazy<DynamicWireFactory>(() => new DynamicWireFactory(), true);

		#endregion

		#region Wiring

		// Cache for created dynamic wire factories
		DynamicWireFactoryCache _wireFactoryCache = new DynamicWireFactoryCache();

		/// <summary>
		/// Creates a unique wire cache key for a delegate wire factory.
		/// </summary>
		/// <param name="componentType">Type of server component.</param>
		/// <param name="delegateType">Delegate type of the wire.</param>
		/// <param name="isEvent">Sets if the wire type is for a event (if false, the wire type must be of a delegate property).</param>
		/// <returns>Unique key.</returns>
		private string CreateKeyForWire(Type componentType, Type delegateType, bool isEvent)
		{
			var wireKeyBuilder = new StringBuilder();
			wireKeyBuilder.Append(isEvent ? "E|" : "D|");
			wireKeyBuilder.Append(componentType.FullName);
			wireKeyBuilder.Append("|");
			wireKeyBuilder.Append(delegateType.FullName);

			return wireKeyBuilder.ToString();
		}

		private DynamicWireBase CreateWire(Type componentType, string delegateMemberName, bool isEvent)
		{
			if (componentType == null)
				throw new ArgumentNullException("componentType");

			if (string.IsNullOrEmpty(delegateMemberName))
				throw new ArgumentException(LanguageResource.ArgumentException_OutPutPinNameMissing, "delegateMemberName");

			var delegateType = GetDelegateType(componentType, delegateMemberName, isEvent);
			var createDynamicWire = GetOrCreateFactoryMethod(componentType, delegateType, isEvent);
			return createDynamicWire(isEvent);
		}

		private DynamicWireBase CreateWire(Type componentType, Type delegateType)
		{
			var createDynamicWire = GetOrCreateFactoryMethod(componentType, delegateType, false);
			return createDynamicWire(false);
		}

		private DynamicWireFactoryMethod GetOrCreateFactoryMethod(Type componentType, Type delegateType, bool isEvent)
		{
			if (componentType == null)
				throw new ArgumentNullException("componentType");

			if (delegateType == null)
				throw new ArgumentNullException("delegateType");

			// look for cached factory method value
			var key = CreateKeyForWire(componentType, delegateType, isEvent);
			return _wireFactoryCache.GetOrAdd(key, x =>
			{
				// create wire factory method
				var methodInfo = CreateDynamicWireGenericMethodInfo.MakeGenericMethod(delegateType);
				var dynamicWireFactoryMethod = methodInfo.CreateDelegate<DynamicWireFactoryMethod>(this);
				return dynamicWireFactoryMethod;
			});
		}

		/// <summary>
		/// <see cref="MethodInfo"/> for <see cref="CreateDynamicWire{T}"/> method.
		/// </summary>
		private static MethodInfo CreateDynamicWireGenericMethodInfo = typeof(DynamicWireFactory).GetMethod("CreateDynamicWire",
			BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);

		/// <summary>
		/// Creates strongly-typed dynamic wire for event handler or delegate.
		/// </summary>
		/// <typeparam name="T">Delegate type.</typeparam>
		/// <param name="isEvent">True if delegate is an event handler.</param>
		/// <returns>Dynamic wire instance.</returns>
		private DynamicWireBase CreateDynamicWire<T>(bool isEvent)
		{
			return isEvent ? new DynamicEventWire<T>() as DynamicWireBase : new DynamicWire<T>();
		}

		/// <summary>
		/// Gets the delegate type of a component´s event or delegate property.
		/// </summary>
		/// <param name="componentType">Component type</param>
		/// <param name="delegateMemberName">Event name or name of the delegate property</param>
		/// <param name="isEvent">Sets if the member is a event (if false, the memeber must be a delegate property)</param>
		/// <returns>Delegate type</returns>
		private Type GetDelegateType(Type componentType, string delegateMemberName, bool isEvent)
		{
			if (isEvent)
			{
				var eventInfo = componentType.GetEvent(delegateMemberName);
				return eventInfo.EventHandlerType;
			}

			var delegatePropInfo = componentType.GetProperty(delegateMemberName);
			return delegatePropInfo.PropertyType;
		}

		#endregion

		/// <summary>
		/// Creates a dynamic wire for a specified event or delegate property of a component.
		/// </summary>
		/// <param name="componentType">Component type.</param>
		/// <param name="delegateMemberName">Event name or name of the delegate property.</param>
		/// <param name="isEvent">Sets if the member is a event (if false, the memeber must be a delegate property).</param>
		/// <returns>Instance of the created dynamic wire type (ready to use).</returns>
		public static DynamicWireBase CreateDynamicWire(Type componentType, string delegateMemberName, bool isEvent)
		{
			return Instance.Value.CreateWire(componentType, delegateMemberName, isEvent);
		}

		/// <summary>
		/// Creates a dynamic wire for a specified event or delegate property of a component.
		/// </summary>
		/// <param name="componentType">Component type.</param>
		/// <param name="delegateType">Type of the delegate.</param>
		/// <returns>Instance of the created dynamic wire type (ready to use).</returns>
		public static DynamicWireBase CreateDynamicWire(Type componentType, Type delegateType)
		{
			return Instance.Value.CreateWire(componentType, delegateType);
		}

		/// <summary>
		/// Extracts the <see cref="DynamicWireBase"/> from the given dynamic-invocable delegate.
		/// </summary>
		/// <param name="delegate">The delegate to extract.</param>
		/// <returns></returns>
		public static DynamicWireBase GetDynamicWire(Delegate @delegate)
		{
			if (@delegate == null)
			{
				return null;
			}

			var target = @delegate.Target as DynamicWireBase;
			if (target != null)
			{
				return target;
			}

			var closure = @delegate.Target as System.Runtime.CompilerServices.Closure;
			if (closure != null && closure.Constants != null)
			{
				return closure.Constants.OfType<DynamicWireBase>().First();
			}

			return null;
		}

		/// <summary>
		/// Builds the strong-typed delegate for the dynamicInvoke: object DynamicInvoke(object[] args);
		/// </summary>
		/// <remarks>
		/// Relies on the compiled LINQ expressions. Delegate Target property isn't equal to the "target" parameter.
		/// </remarks>
		/// <typeparam name="T">Delegate type.</typeparam>
		/// <param name="dynamicInvoke"><see cref="MethodInfo"/> for the DynamicInvoke(object[] args) method.</param>
		/// <param name="target">Target instance.</param>
		/// <returns>Strong-typed delegate.</returns>
		public static T BuildDelegate<T>(MethodInfo dynamicInvoke, object target = null)
		{
			// validate generic argument
			if (!typeof(Delegate).IsAssignableFrom(typeof(T)))
			{
				throw new ApplicationException(string.Format(LanguageResource.ApplicationException_TypeIsNotDelegate, typeof(T).FullName));
			}

			// get delegate MethodInfo
			var delegateType = typeof(T);
			var delegateInvokeMethod = delegateType.GetMethod("Invoke");

			// var parameters = new object[] { (object)arg1, (object)arg2, ... };
			var parameters = delegateInvokeMethod.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
			var objectParameters = parameters.Select(p => Expression.Convert(p, typeof(object)));
			var parametersArray = Expression.NewArrayInit(typeof(object), objectParameters.OfType<Expression>().ToArray());

			// invokeClientDelegate(parameters);
			var callExpression = Expression.Call
			(
				Expression.Constant(target),
				dynamicInvoke,
				new Expression[] { parametersArray }
			);

			// convert return value, if any
			var resultExpression = callExpression as Expression;
			if (delegateInvokeMethod.ReturnType != typeof(void))
			{
				resultExpression = Expression.Convert(callExpression, delegateInvokeMethod.ReturnType);
			}

			// create expression and compile delegate
			var expression = Expression.Lambda<T>(resultExpression, parameters);
			return expression.Compile();
		}

		/// <summary>
		/// Builds the strong-typed delegate bound to the given target instance
		/// for the dynamicInvoke method: object DynamicInvoke(object[] args);
		/// </summary>
		/// <remarks>
		/// Relies on the dynamic methods. Delegate Target property is equal to the "target" parameter.
		/// Doesn't support static methods.
		/// </remarks>
		/// <typeparam name="T">Delegate type.</typeparam>
		/// <param name="dynamicInvoke"><see cref="MethodInfo"/> for the DynamicInvoke(object[] args) method.</param>
		/// <param name="target">Target instance.</param>
		/// <returns>Strong-typed delegate.</returns>
		public static T BuildInstanceDelegate<T>(MethodInfo dynamicInvoke, object target)
		{
			// validate generic argument
			if (!typeof(Delegate).IsAssignableFrom(typeof(T)))
			{
				throw new ApplicationException(string.Format(LanguageResource.ApplicationException_TypeIsNotDelegate, typeof(T).FullName));
			}

			// reflect delegate type to get parameters and method return type
			var delegateType = typeof(T);
			var invokeMethod = delegateType.GetMethod("Invoke");

			// figure out parameters
			var paramTypeList = invokeMethod.GetParameters().Select(p => p.ParameterType).ToList();
			var paramCount = paramTypeList.Count;
			var ownerType = target.GetType();
 			paramTypeList.Insert(0, ownerType);
			var paramTypes = paramTypeList.ToArray();
			var typedInvoke = new DynamicMethod("TypedInvoke", invokeMethod.ReturnType, paramTypes, ownerType);

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
			ilGenerator.Emit(OpCodes.Call, dynamicInvoke);

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
			var result = typedInvoke.CreateDelegate(delegateType, target);
			return (T)(object)result;
		}
	}
}

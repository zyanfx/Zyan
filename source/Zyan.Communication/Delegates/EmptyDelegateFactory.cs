using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Delegates
{
	#region Synonyms
#if !FX3
	using DelegateFactoryCache = System.Collections.Concurrent.ConcurrentDictionary<Type, Func<Delegate>>;
#else
	using DelegateFactoryCache = Zyan.Communication.Toolbox.ConcurrentDictionary<Type, Func<Delegate>>;
#endif
	#endregion

	/// <summary>
	/// Creates empty delegates of any given type.
	/// </summary>
	/// <remarks>
	/// An empty delegate is a delegate to a method that does nothing.
	/// For example, empty EventHandler delegate is (sender, args) => {}.
	/// </remarks>
	public class EmptyDelegateFactory
	{
		/// <summary>
		/// Creates the empty delegate of the given type in non-generic fashion.
		/// </summary>
		/// <param name="delegateType">Type of the delegate.</param>
		/// <returns>A fresh copy of the empty delegate.</returns>
		public static Delegate CreateEmptyDelegate(Type delegateType)
		{
			// get CreateEmptyDelegate<delegateType>()
			var createEmptyDelegate = cachedDelegates.GetOrAdd(delegateType, type =>
			{
				var methodInfo = createEmptyDelegateMethod.MakeGenericMethod(type);
				return methodInfo.CreateDelegate<Func<Delegate>>();
			});

			// invoke it
			return createEmptyDelegate();
		}

		/// <summary>
		/// Creates the empty delegate in a generic fashion.
		/// </summary>
		/// <typeparam name="TDelegate">The type of the delegate.</typeparam>
		/// <returns>A fresh copy of the empty delegate.</returns>
		/// <exception cref="System.InvalidOperationException">is thrown if the type is not a delegate type.</exception>
		public static TDelegate CreateEmptyDelegate<TDelegate>()
		{
			if (!typeof(Delegate).IsAssignableFrom(typeof(TDelegate)))
			{
				throw new InvalidOperationException(typeof(TDelegate) + " is not a delegate type.");
			}

			return InternalDelegateFactory<TDelegate>.EmptyDelegate;
		}

		private static DelegateFactoryCache cachedDelegates = new DelegateFactoryCache();

		private static MethodInfo createEmptyDelegateMethod = typeof(EmptyDelegateFactory).GetMethod("CreateEmptyDelegate", BindingFlags.Static | BindingFlags.Public, null, new Type[0], null);

		private class InternalDelegateFactory<TDelegate>
		{
			#region Platform-specific CreateEmptyDelegateExpression method
#if !FX3
			private static Expression<TDelegate> CreateEmptyDelegateExpression()
			{
				var delegateType = typeof(TDelegate);
				var invokeMethod = delegateType.GetMethod("Invoke");
				var parameters = invokeMethod.GetParameters().Select(p => Expression.Parameter(p.ParameterType)).ToArray();
				return Expression.Lambda<TDelegate>(Expression.Default(invokeMethod.ReturnType), parameters);
			}
#else
			private static Expression<TDelegate> CreateEmptyDelegateExpression()
			{
				var delegateType = typeof(TDelegate);
				var invokeMethod = delegateType.GetMethod("Invoke");
				var parameters = invokeMethod.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

				var defaultExpression = default(Expression);
				if (invokeMethod.ReturnType == typeof(void))
				{
					// in C# 3.0, there is no way to create empty expression of the type Void
					Expression<Action> doNothing = () => DoNothing();
					defaultExpression = doNothing.Body;
				}
				else
				{
					// in C# 3.0, there is no Expression.Default, so we use Expression.Constant instead
					defaultExpression = Expression.Constant(GetDefaultValue(invokeMethod.ReturnType), invokeMethod.ReturnType);
				}

				return Expression.Lambda<TDelegate>(defaultExpression, parameters);
			}

			private static void DoNothing()
			{
			}

			static object GetDefaultValue(Type t)
			{
				if (t.IsValueType)
				{
					return Activator.CreateInstance(t);
				}
				else
				{
					return null;
				}
			}
#endif
			#endregion

			private static TDelegate emptyDelegate = CreateEmptyDelegateExpression().Compile();

			public static TDelegate EmptyDelegate
			{
				get
				{
					// convert emptyDelegate to a generic Delegate type
					var empty = (Delegate)(object)emptyDelegate;

					// create a fresh copy of it
					return (TDelegate)(object)Delegate.CreateDelegate(typeof(TDelegate), empty, "Invoke");
				}
			}
		}
	}
}

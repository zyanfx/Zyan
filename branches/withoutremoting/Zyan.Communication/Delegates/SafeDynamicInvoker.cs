using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.Threading;
using Zyan.Communication.Threading;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.Delegates
{
	using DelegateFactoryCache = ConcurrentDictionary<Type, Func<Delegate, object[], object>>;

	/// <summary>
	/// A safer replacement for the Delegate.DynamicInvoke method.
	/// </summary>
	public static class SafeDynamicInvoker
	{
		private static DelegateFactoryCache dynamicInvokers = new DelegateFactoryCache();

		/// <summary>
		/// Gets the dynamic invoker for the given delegate.
		/// </summary>
		/// <remarks>
		/// Dynamic invokers uses runtime code generation instead of the late binding of the Delegate.DynamicInvoke.
		/// It doesn't wrap the exception thrown by the delegate into <see cref="TargetInvocationException"/>.
		/// On .NET runtime (versions 3.5, 4.0 and 4.5), this invoker is 10-12 times faster than the original DynamicInvoke.
		/// On Mono runtime (version 2.10.5), it performs 1.5-2 times slower than the original DynamicInvoke.
		/// </remarks>
		/// <param name="deleg">The delegate to invoke.</param>
		/// <returns>Dynamic invocation function.</returns>
		public static Func<Delegate, object[], object> GetDynamicInvoker(this Delegate deleg)
		{
			return dynamicInvokers.GetOrAdd(deleg.GetType(), delegateType =>
			{
				// Building parameters for Expression<Func<Delegate, object[], object>>
				var delegateParameter = Expression.Parameter(typeof(Delegate), "some");
				var argsParameter = Expression.Parameter(typeof(object[]), "args");

				// Building parameters for the real delegate
				var delegateMethod = delegateType.GetMethod("Invoke");
				var delegateParameters = delegateMethod.GetParameters();
				var paramExpressions = new List<Expression>();
				for (var index = 0; index < delegateParameters.Length; index++)
				{
					var indexExpr = Expression.ArrayIndex(argsParameter, Expression.Constant(index));
					var paramType = delegateParameters[index].ParameterType;
					var convertExpr = Expression.Convert(indexExpr, paramType);
					paramExpressions.Add(convertExpr);
				}

				// Func
				if (delegateMethod.ReturnType != typeof(void))
				{
					// Build delegate invocation expression
					var funcExpr = Expression.Lambda<Func<Delegate, object[], object>>
					(
						Expression.Convert
						(
							Expression.Invoke
							(
								// (RealDelegateType)delegateParameter
								Expression.Convert
								(
									delegateParameter,
									delegateType
								),
								// (RealType)arg[i]
								paramExpressions
							),
							typeof(object)
						),
						new[] { delegateParameter, argsParameter }
					);

					// Build invocation delegate
					return funcExpr.Compile();
				}

				// Action (return type is void)
				var actionExpr = Expression.Lambda<Action<Delegate, object[]>>
				(
					Expression.Invoke
					(
						// (RealDelegateType)delegateParameter
						Expression.Convert
						(
							delegateParameter,
							delegateType
						),
						// (RealType)arg[i]
						paramExpressions
					),
					new[] { delegateParameter, argsParameter }
				);

				// turn Action<Delegate, object[]> into Func<Delegate, object[], object>
				var actionInvoker = actionExpr.Compile();
				return (d, a) =>
				{
					actionInvoker(d, a);
					return null;
				};
			});
		}

		/// <summary>
		/// Dynamically invokes the method represented by the given delegate. The delegate can be null. 
		/// </summary>
		/// <remarks>
		/// Ensures that all delegates of the invocation list are called (even if some exceptions occured).
		/// If several delegates throw exceptions, then the first exception is rethrown.
		/// In .NET 4.0, all exceptions are aggregated into one AggregateException.
		/// </remarks>
		/// <param name="deleg">The delegate to invoke.</param>
		/// <param name="args">The arguments for the delegate.</param>
		/// <returns>The return value of the delegate</returns>
		public static object SafeDynamicInvoke(this Delegate deleg, params object[] args)
		{
			if (deleg == null)
			{
				return null;
			}

			// Fire invocation delegate in a safe manner
			return InternalSafeInvoke(deleg, args);
		}

		private static object InternalSafeInvoke(Delegate deleg, object[] arguments)
		{
			var dynamicInvoker = GetDynamicInvoker(deleg);
			var invocationList = deleg.GetInvocationList();
			if (invocationList.Length < 2)
			{
				// nothing to worry about, invoke as is
				return dynamicInvoker(deleg, arguments);
			}

			// run all delegates and gather exceptions being thrown
			var exceptions = new List<Exception>();
			var result = default(object);
			foreach (var d in invocationList)
			{
				try
				{
					result = dynamicInvoker(d, arguments);
				}
				catch (Exception ex)
				{
					exceptions.Add(ex.PreserveStackTrace());
				}
			}

			// return the last result or rethrow exceptions
			if (!exceptions.Any())
			{
				return result;
			}

#if !FX3
			if (exceptions.Count == 1)
			{
				throw exceptions.First();
			}

			// .NET 4.0 only
			throw new AggregateException(exceptions);
#else
			throw exceptions.First();
#endif
		}

		/// <summary>
		/// Dynamically invokes the method represented by the given delegate as a one-way method. The delegate can be null. 
		/// </summary>
		/// <param name="deleg">The delegate to invoke.</param>
		/// <param name="arguments">The arguments.</param>
		public static void OneWayDynamicInvoke(this Delegate deleg, object[] arguments)
		{
			if (deleg == null)
			{
				return;
			}

			var dynamicInvoker = GetDynamicInvoker(deleg);
			var invocationList = deleg.GetInvocationList();
			foreach (var d in invocationList)
			{
				// implemented using custom thread pool
				ThreadPool.QueueUserWorkItem(x =>
				{
					try
					{
						dynamicInvoker(d, arguments);
					}
					catch (Exception ex)
					{
						Trace.WriteLine("Exception in an event handler: {0}", ex);
					}
				});
			}
		}

		private static IThreadPool threadPool = new SimpleLockThreadPool();

		/// <summary>
		/// Gets or sets the thread pool used to send server events to remote subscribers.
		/// </summary>
		/// <remarks>
		/// Assign this property to an instance of the <see cref="ClrThreadPool"/> class
		/// to fall back to the standard <see cref="System.Threading.ThreadPool"/>.
		/// </remarks>
		public static IThreadPool ThreadPool
		{
			get { return threadPool; }
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}

				threadPool = value;
			}
		}
	}
}

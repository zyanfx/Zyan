using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Zyan.Communication
{
	/// <summary>
	/// Strongly typed wrapper for DelegateInterceptor.
	/// </summary>
	/// <typeparam name="T">Delegate type.</typeparam>
	public class DynamicWire<T>
	{
		/// <summary>
		/// Initializes <see cref="DynamicWire{T}"/> instance.
		/// </summary>
		public DynamicWire()
		{
			if (!typeof(Delegate).IsAssignableFrom(typeof(T)))
			{
				throw new ApplicationException("Type is not delegate: " + typeof(T).FullName);
			}
		}

		/// <summary>
		/// Client delegate interceptor.
		/// </summary>
		public DelegateInterceptor Interceptor { get; set; }

		/// <summary>
		/// Invokes interceptor's delegate.
		/// </summary>
		/// <param name="args">Method parameters.</param>
		/// <returns>Method return value.</returns>
		protected virtual object InvokeClientDelegate(params object[] args)
		{
			return Interceptor.InvokeClientDelegate(args);
		}

		static MethodInfo InvokeClientDelegateMethodInfo = 
			typeof(DynamicWire<T>).GetMethod("InvokeClientDelegate",
				BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Dynamic wire delegate.
		/// </summary>
		public T In
		{
			get
			{
				if (delegateValue == null)
				{
					delegateValue = BuildDelegate();
				}

				return delegateValue;
			}
		}

		private T delegateValue;

		private T BuildDelegate()
		{
			var delegateType = typeof(T);
			var invokeMethod = delegateType.GetMethod("Invoke");

			// var parameters = new object[] { (object)arg1, (object)arg2, ... };
			var parameters = invokeMethod.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
			var objectParameters = parameters.Select(p => Expression.Convert(p, typeof(object)));
			var parametersArray = Expression.NewArrayInit(typeof(object), objectParameters.OfType<Expression>().ToArray());

			// this.InvokeClientDelegate(parameters);
			var resultExpression = default(Expression);
			var callExpression = Expression.Call
			(
				Expression.Constant(this, GetType()),
				InvokeClientDelegateMethodInfo,
				new Expression[] { parametersArray }
			);

			// convert return value, if any
			resultExpression = callExpression;
			if (invokeMethod.ReturnType != typeof(void))
			{
				resultExpression = Expression.Convert(callExpression, invokeMethod.ReturnType);
			}

			// create expression and compile delegate
			var expression = Expression.Lambda<T>(resultExpression, parameters);
			return expression.Compile();
		}
	}
}

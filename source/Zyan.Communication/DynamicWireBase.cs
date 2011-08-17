using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Zyan.Communication
{
	/// <summary>
	/// Base class for dynamic wires.
	/// </summary>
	public abstract class DynamicWireBase
	{
		/// <summary>
		/// Client delegate interceptor.
		/// </summary>
		public DelegateInterceptor Interceptor { get; set; }

		/// <summary>
		/// Invokes intercepted delegate.
		/// </summary>
		/// <param name="args">Delegate parameters.</param>
		/// <returns>Delegate return value.</returns>
		protected virtual object InvokeClientDelegate(params object[] args)
		{
			return Interceptor.InvokeClientDelegate(args);
		}

		/// <summary>
		/// <see cref="MethodInfo"/> for <see cref="InvokeClientDelegate"/> method.
		/// </summary>
		protected static readonly MethodInfo InvokeClientDelegateMethodInfo = 
			typeof(DynamicWireBase).GetMethod("InvokeClientDelegate", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Gets the untyped In delegate.
		/// </summary>
		public abstract Delegate InDelegate { get; }

		/// <summary>
		/// Builds strong-typed delegate of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Delegate type.</typeparam>
		/// <returns>Delegate to call <see cref="InvokeClientDelegate"/> method.</returns>
		protected T BuildDelegate<T>()
		{
			// validate generic argument
			if (!typeof(Delegate).IsAssignableFrom(typeof(T)))
			{
				throw new ApplicationException("Type is not delegate: " + typeof(T).FullName);
			}

			// get delegate MethodInfo
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

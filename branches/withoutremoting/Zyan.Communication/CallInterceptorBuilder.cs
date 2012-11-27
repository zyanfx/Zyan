using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication
{
	/// <summary>
	/// Strong-typed builder for individual call interceptors.
	/// </summary>
	public class CallInterceptorBuilder<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CallInterceptorBuilder{T}"/> class.
		/// </summary>
		/// <param name="uniqueName">Unique name of the intercepted component.</param>
		public CallInterceptorBuilder(string uniqueName)
		{
			UniqueName = string.IsNullOrEmpty(uniqueName) ? typeof(T).FullName : uniqueName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CallInterceptorBuilder{T}"/> class.
		/// </summary>
		public CallInterceptorBuilder()
			: this(null)
		{
		}

		/// <summary>
		/// Gets the unique name of the intercepted component.
		/// </summary>
		public string UniqueName { get; private set; }

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Action(Expression<Action<T>> expression, CallInterceptionDelegate handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), UniqueName, memberType, memberName, new Type[0], handler);
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Func<T1>(Expression<Func<T, T1>> expression, Func<CallInterceptionData, T1> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), UniqueName, memberType, memberName, new Type[0],
				data => data.ReturnValue = handler(data));
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Action<T1>(Expression<Action<T, T1>> expression, Action<CallInterceptionData, T1> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), memberType, memberName, new[] { typeof(T1) },
				data => handler(data, (T1)data.Parameters[0]));
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Func<T1, T2>(Expression<Func<T, T1, T2>> expression, Func<CallInterceptionData, T1, T2> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), memberType, memberName, new[] { typeof(T1) },
				data => data.ReturnValue = handler(data, (T1)data.Parameters[0]));
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Action<T1, T2>(Expression<Action<T, T1, T2>> expression, Action<CallInterceptionData, T1, T2> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), memberType, memberName, new[] { typeof(T1), typeof(T2) },
				data => handler(data, (T1)data.Parameters[0], (T2)data.Parameters[1]));
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Func<T1, T2, T3>(Expression<Func<T, T1, T2, T3>> expression, Func<CallInterceptionData, T1, T2, T3> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), memberType, memberName, new[] { typeof(T1), typeof(T2) },
				data => data.ReturnValue = handler(data, (T1)data.Parameters[0], (T2)data.Parameters[1]));
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Action<T1, T2, T3>(Expression<Action<T, T1, T2, T3>> expression, Action<CallInterceptionData, T1, T2, T3> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), memberType, memberName, new[] { typeof(T1), typeof(T2), typeof(T3) },
				data => handler(data, (T1)data.Parameters[0], (T2)data.Parameters[1], (T3)data.Parameters[2]));
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Func<T1, T2, T3, T4>(Expression<Func<T, T1, T2, T3, T4>> expression, Func<CallInterceptionData, T1, T2, T3, T4> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), memberType, memberName, new[] { typeof(T1), typeof(T2), typeof(T3) },
				data => data.ReturnValue = handler(data, (T1)data.Parameters[0], (T2)data.Parameters[1], (T3)data.Parameters[2]));
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Action<T1, T2, T3, T4>(Expression<Action<T, T1, T2, T3, T4>> expression, Action<CallInterceptionData, T1, T2, T3, T4> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), memberType, memberName, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
				data => handler(data, (T1)data.Parameters[0], (T2)data.Parameters[1], (T3)data.Parameters[2], (T4)data.Parameters[3]));
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Func<T1, T2, T3, T4, T5>(Expression<Func<T, T1, T2, T3, T4, T5>> expression, Func<CallInterceptionData, T1, T2, T3, T4, T5> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), memberType, memberName, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
				data => data.ReturnValue = handler(data, (T1)data.Parameters[0], (T2)data.Parameters[1], (T3)data.Parameters[2], (T4)data.Parameters[3]));
		}

		/// <summary>
		/// Creates new CallInterceptor for the given method.
		/// </summary>
		/// <param name="expression">LINQ expression of the method to intercept.</param>
		/// <param name="handler">Interception handler.</param>
		public CallInterceptor Action<T1, T2, T3, T4, T5>(Expression<Action<T, T1, T2, T3, T4, T5>> expression, Action<CallInterceptionData, T1, T2, T3, T4, T5> handler)
		{
			CheckNotNull(handler);

			MemberTypes memberType;
			string memberName;
			Parse(expression, out memberType, out memberName);

			return new CallInterceptor(typeof(T), memberType, memberName, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) },
				data => handler(data, (T1)data.Parameters[0], (T2)data.Parameters[1], (T3)data.Parameters[2], (T4)data.Parameters[3], (T5)data.Parameters[4]));
		}

		/// <summary>
		/// Checks whether the given handler is not null.
		/// </summary>
		private static void CheckNotNull<THandler>(THandler handler)
		{
			CallInterceptorHelper<T>.CheckNotNull(handler);
		}

		/// <summary>
		/// Parses lambda expression and extracts memberType and memberName.
		/// </summary>
		private static void Parse(LambdaExpression lambda, out MemberTypes memberType, out string memberName)
		{
			CallInterceptorHelper<T>.Parse(lambda, out memberType, out memberName);
		}
	}
}

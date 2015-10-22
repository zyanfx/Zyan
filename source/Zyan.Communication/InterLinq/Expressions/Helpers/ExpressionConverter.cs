using System.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using Zyan.InterLinq.Expressions.SerializableTypes;
using Zyan.InterLinq.Types.Anonymous;

namespace Zyan.InterLinq.Expressions.Helpers
{
	/// <summary>
	/// This class is an <see cref="ExpressionVisitor"/> implementation
	/// used to convert a <see cref="Expression"/> to a
	/// <see cref="SerializableExpression"/>.
	/// </summary>
	/// <seealso cref="ExpressionVisitor"/>
	public class ExpressionConverter : ExpressionVisitor
	{
		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="expression"><see cref="Expression"/> to convert.</param>
		public ExpressionConverter(Expression expression) : base(expression) { }

		#endregion

		#region New Methods

		/// <summary>
		/// Converts an <see cref="Expression"/> to a <see cref="SerializableExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="Expression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableExpression"/>.</returns>
		internal SerializableExpression Convert(Expression expression)
		{
			return Convert<SerializableExpression>(expression);
		}

		/// <summary>
		/// Converts an <see cref="Expression"/> to <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Target type.</typeparam>
		/// <param name="expression"><see cref="Expression"/> to convert.</param>
		/// <returns>Returns the converted <typeparamref name="T"/>.</returns>
		internal T Convert<T>(Expression expression)
		{
			return Visit<T>(expression);
		}

		/// <summary>
		/// Converts an <see cref="IEnumerable"/> to a <see cref="ReadOnlyCollection{T}"/>.
		/// </summary>
		/// <typeparam name="T">Target type.</typeparam>
		/// <param name="enumerable"><see cref="IEnumerable"/> to convert.</param>
		/// <returns>Returns the converted <see cref="ReadOnlyCollection{T}"/>.</returns>
		internal ReadOnlyCollection<T> ConvertCollection<T>(IEnumerable enumerable)
		{
			return new ReadOnlyCollection<T>(VisitCollection<T>(enumerable));
		}

		/// <summary>
		/// Converts an <see cref="IEnumerable"/> to a <see cref="ReadOnlyCollection{T}"/>.
		/// </summary>
		/// <remarks>
		/// This method is called for classes like <see cref="SerializableElementInit"/>,
		/// <see cref="SerializableMemberBinding"/>, etc.
		/// </remarks>
		/// <typeparam name="T">Target type.</typeparam>
		/// <param name="enumerable"><see cref="IEnumerable"/> to convert.</param>
		/// <returns>Returns the converted <see cref="ReadOnlyCollection{T}"/>.</returns>
		internal ReadOnlyCollection<T> ConvertToSerializableObjectCollection<T>(IEnumerable enumerable)
		{
			return new ReadOnlyCollection<T>(VisitObjectCollection<T>(enumerable));
		}

		#endregion

		#region Conversion Methods

		/// <summary>
		/// Converts a <see cref="BinaryExpression"/> to a
		/// <see cref="SerializableBinaryExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="BinaryExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableBinaryExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitBinaryExpression"/>
		protected override object VisitBinaryExpression(BinaryExpression expression)
		{
			return new SerializableBinaryExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="ConditionalExpression"/> to a
		/// <see cref="SerializableConditionalExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="ConditionalExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableConditionalExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitConditionalExpression"/>
		protected override object VisitConditionalExpression(ConditionalExpression expression)
		{
			return new SerializableConditionalExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="ConstantExpression"/> to a
		/// <see cref="SerializableConstantExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="ConstantExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableConstantExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitConstantExpression"/>
		protected override object VisitConstantExpression(ConstantExpression expression)
		{
			return new SerializableConstantExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="Expression{T}"/> to a
		/// <see cref="SerializableExpressionTyped"/>.
		/// </summary>
		/// <param name="expression"><see cref="Expression{T}"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableExpressionTyped"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitTypedExpression{T}"/>
		protected override object VisitTypedExpression<T>(Expression<T> expression)
		{
			return new SerializableExpressionTyped(expression, typeof(T), this);
		}

		/// <summary>
		/// Converts a <see cref="InvocationExpression"/> to a
		/// <see cref="SerializableInvocationExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="InvocationExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableInvocationExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitInvocationExpression"/>
		protected override object VisitInvocationExpression(InvocationExpression expression)
		{
			return new SerializableInvocationExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="LambdaExpression"/> to a
		/// <see cref="SerializableLambdaExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="LambdaExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableLambdaExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitLambdaExpression"/>
		protected override object VisitLambdaExpression(LambdaExpression expression)
		{
			return new SerializableLambdaExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="ListInitExpression"/> to a
		/// <see cref="SerializableListInitExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="ListInitExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableListInitExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitListInitExpression"/>
		protected override object VisitListInitExpression(ListInitExpression expression)
		{
			return new SerializableListInitExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="MemberExpression"/> to a
		/// <see cref="SerializableMemberExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="MemberExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableMemberExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberExpression"/>
		protected override object VisitMemberExpression(MemberExpression expression)
		{
			// If the member is a display class member (non-serializable, internal)
			// replace the whole member expression by a constant expression containing
			// the display class member value.
			if (expression.Expression != null && expression.Expression.Type != null && expression.Expression.Type.IsDisplayClass())
			{
				// C# 6.0 can generate nested display classes
				if (expression.Expression is MemberExpression)
				{
					var scx = VisitMemberExpression((MemberExpression)expression.Expression) as SerializableConstantExpression;
					if (scx != null)
					{
						object value = ((FieldInfo)expression.Member).GetValue(scx.Value);
						return Visit(Expression.Constant(value));
					}
				}
				else if (expression.Expression is ConstantExpression)
				{
					var innerExpression = (ConstantExpression)expression.Expression;
					if (innerExpression.Type.IsDisplayClass())
					{
						object value = ((FieldInfo)expression.Member).GetValue(innerExpression.Value);
						return Visit(Expression.Constant(value));
					}
				}
			}

			return new SerializableMemberExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="MemberInitExpression"/> to a
		/// <see cref="SerializableMemberInitExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="MemberInitExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableMemberInitExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberInitExpression"/>
		protected override object VisitMemberInitExpression(MemberInitExpression expression)
		{
			return new SerializableMemberInitExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="MethodCallExpression"/> to a
		/// <see cref="SerializableMethodCallExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="MethodCallExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableMethodCallExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMethodCallExpression"/>
		protected override object VisitMethodCallExpression(MethodCallExpression expression)
		{
			return new SerializableMethodCallExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="NewArrayExpression"/> to a
		/// <see cref="SerializableNewArrayExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="NewArrayExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableNewArrayExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitNewArrayExpression"/>
		protected override object VisitNewArrayExpression(NewArrayExpression expression)
		{
			return new SerializableNewArrayExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="NewExpression"/> to a
		/// <see cref="SerializableNewExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="NewExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableNewExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitNewExpression"/>
		protected override object VisitNewExpression(NewExpression expression)
		{
			return new SerializableNewExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="ParameterExpression"/> to a
		/// <see cref="SerializableParameterExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="ParameterExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableParameterExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitParameterExpression"/>
		protected override object VisitParameterExpression(ParameterExpression expression)
		{
			return new SerializableParameterExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="TypeBinaryExpression"/> to a
		/// <see cref="SerializableTypeBinaryExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="TypeBinaryExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableTypeBinaryExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitTypeBinaryExpression"/>
		protected override object VisitTypeBinaryExpression(TypeBinaryExpression expression)
		{
			return new SerializableTypeBinaryExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="UnaryExpression"/> to a
		/// <see cref="SerializableUnaryExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="UnaryExpression"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableUnaryExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitUnaryExpression"/>
		protected override object VisitUnaryExpression(UnaryExpression expression)
		{
			return new SerializableUnaryExpression(expression, this);
		}

		/// <summary>
		/// Converts a <see cref="ElementInit"/> to a
		/// <see cref="SerializableElementInit"/>.
		/// </summary>
		/// <param name="elementInit"><see cref="ElementInit"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableElementInit"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitElementInit"/>
		protected override object VisitElementInit(ElementInit elementInit)
		{
			return new SerializableElementInit(elementInit, this);
		}

		/// <summary>
		/// Converts a <see cref="MemberAssignment"/> to a
		/// <see cref="SerializableMemberAssignment"/>.
		/// </summary>
		/// <param name="memberAssignment"><see cref="MemberAssignment"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableMemberAssignment"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberAssignment"/>
		protected override object VisitMemberAssignment(MemberAssignment memberAssignment)
		{
			return new SerializableMemberAssignment(memberAssignment, this);
		}

		/// <summary>
		/// Converts a <see cref="MemberListBinding"/> to a
		/// <see cref="SerializableMemberListBinding"/>.
		/// </summary>
		/// <param name="memberListBinding"><see cref="MemberListBinding"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableMemberListBinding"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberListBinding"/>
		protected override object VisitMemberListBinding(MemberListBinding memberListBinding)
		{
			return new SerializableMemberListBinding(memberListBinding, this);
		}

		/// <summary>
		/// Converts a <see cref="MemberMemberBinding"/> to a
		/// <see cref="SerializableMemberMemberBinding"/>.
		/// </summary>
		/// <param name="memberMemberBinding"><see cref="MemberMemberBinding"/> to convert.</param>
		/// <returns>Returns the converted <see cref="SerializableMemberMemberBinding"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberMemberBinding"/>
		protected override object VisitMemberMemberBinding(MemberMemberBinding memberMemberBinding)
		{
			return new SerializableMemberMemberBinding(memberMemberBinding, this);
		}

		#endregion
	}
}

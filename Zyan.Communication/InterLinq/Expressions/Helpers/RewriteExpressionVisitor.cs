using System.Linq.Expressions;

namespace Zyan.InterLinq.Expressions.Helpers
{
	/// <summary>
	/// This is a visitor that rewrites an expression tree during visiting.
	/// The result is the same tree like the one, given to the visitor.
	/// </summary>
	/// <seealso cref="ExpressionVisitor"/>
	public class RewriteExpressionVisitor : ExpressionVisitor
	{
		#region Constructor

		/// <summary>
		/// Creates a new <see cref="RewriteExpressionVisitor"/> with an <see cref="Expression"/>.
		/// </summary>
		/// <param name="expression"><see cref="Expression"/> to initialize the visitor.</param>
		public RewriteExpressionVisitor(Expression expression) : base(expression) { }

		#endregion

		/// <summary>
		/// Visit the <see cref="Expression"/> and returns an expression.
		/// </summary>
		/// <param name="expression"><see cref="Expression"/> to execute.</param>
		/// <returns>Returns an <see cref="Expression"/>.</returns>
		protected Expression VisitExpression(Expression expression)
		{
			return Visit<Expression>(expression);
		}

		#region Rewrite Methods

		/// <summary>
		/// Creates a <see cref="BinaryExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="BinaryExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="BinaryExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitBinaryExpression"/>
		protected override object VisitBinaryExpression(BinaryExpression expression)
		{
			return Expression.MakeBinary(expression.NodeType, VisitExpression(expression.Left), VisitExpression(expression.Right), expression.IsLiftedToNull, expression.Method);
		}

		/// <summary>
		/// Creates a <see cref="ConditionalExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="ConditionalExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="ConditionalExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitConditionalExpression"/>
		protected override object VisitConditionalExpression(ConditionalExpression expression)
		{
			return Expression.Condition(VisitExpression(expression.Test), VisitExpression(expression.IfTrue), VisitExpression(expression.IfFalse));
		}

		/// <summary>
		/// Creates a <see cref="ConstantExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="ConstantExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="ConstantExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitConstantExpression"/>
		protected override object VisitConstantExpression(ConstantExpression expression)
		{
			return Expression.Constant(expression.Value, expression.Type);
		}

		/// <summary>
		/// Creates a <see cref="System.Linq.Expressions.Expression{T}"/>.
		/// </summary>
		/// <param name="expression"><see cref="System.Linq.Expressions.Expression{T}"/> to visit.</param>
		/// <returns>Returns a <see cref="System.Linq.Expressions.Expression{T}"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitTypedExpression{T}"/>
		protected override object VisitTypedExpression<T>(Expression<T> expression)
		{
			return Expression.Lambda<T>(VisitExpression(expression.Body), VisitCollection<ParameterExpression>(expression.Parameters));
		}

		/// <summary>
		/// Creates a <see cref="InvocationExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="InvocationExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="InvocationExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitInvocationExpression"/>
		protected override object VisitInvocationExpression(InvocationExpression expression)
		{
			return Expression.Invoke(VisitExpression(expression.Expression), VisitCollection<Expression>(expression.Arguments));
		}

		/// <summary>
		/// Creates a <see cref="LambdaExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="LambdaExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="LambdaExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitLambdaExpression"/>
		protected override object VisitLambdaExpression(LambdaExpression expression)
		{
			return Expression.Lambda(expression.Type, VisitExpression(expression.Body), VisitCollection<ParameterExpression>(expression.Parameters));
		}

		/// <summary>
		/// Creates a <see cref="ListInitExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="ListInitExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="ListInitExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitListInitExpression"/>
		protected override object VisitListInitExpression(ListInitExpression expression)
		{
			return Expression.ListInit(Visit<NewExpression>(expression.NewExpression), VisitObjectCollection<ElementInit>(expression.Initializers));
		}

		/// <summary>
		/// Creates a <see cref="MemberExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="MemberExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="MemberExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberExpression"/>
		protected override object VisitMemberExpression(MemberExpression expression)
		{
			return Expression.MakeMemberAccess(VisitExpression(expression.Expression), expression.Member);
		}

		/// <summary>
		/// Creates a <see cref="MemberInitExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="MemberInitExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="MemberInitExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberInitExpression"/>
		protected override object VisitMemberInitExpression(MemberInitExpression expression)
		{
			return Expression.MemberInit(Visit<NewExpression>(expression.NewExpression), VisitObjectCollection<MemberBinding>(expression.Bindings));
		}

		/// <summary>
		/// Creates a <see cref="MethodCallExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="MethodCallExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="MethodCallExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMethodCallExpression"/>
		protected override object VisitMethodCallExpression(MethodCallExpression expression)
		{
			return Expression.Call(VisitExpression(expression.Object), expression.Method, VisitCollection<Expression>(expression.Arguments));
		}

		/// <summary>
		/// Creates a <see cref="NewArrayExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="NewArrayExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="NewArrayExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitNewArrayExpression"/>
		protected override object VisitNewArrayExpression(NewArrayExpression expression)
		{
			if (expression.NodeType == ExpressionType.NewArrayBounds)
			{
				return Expression.NewArrayBounds(expression.Type, VisitCollection<Expression>(expression.Expressions));
			}
			return Expression.NewArrayInit(expression.Type, VisitCollection<Expression>(expression.Expressions));
		}

		/// <summary>
		/// Creates a <see cref="NewExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="NewExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="NewExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitNewExpression"/>
		protected override object VisitNewExpression(NewExpression expression)
		{
			if (expression.Members == null)
			{
				return Expression.New(expression.Constructor, VisitCollection<Expression>(expression.Arguments));
			}
			return Expression.New(expression.Constructor, VisitCollection<Expression>(expression.Arguments), expression.Members);
		}

		/// <summary>
		/// Creates a <see cref="ParameterExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="ParameterExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="ParameterExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitParameterExpression"/>
		protected override object VisitParameterExpression(ParameterExpression expression)
		{
			return Expression.Parameter(expression.Type, expression.Name);
		}

		/// <summary>
		/// Creates a <see cref="TypeBinaryExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="TypeBinaryExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="TypeBinaryExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitTypeBinaryExpression"/>
		protected override object VisitTypeBinaryExpression(TypeBinaryExpression expression)
		{
			return Expression.TypeIs(VisitExpression(expression.Expression), expression.TypeOperand);
		}

		/// <summary>
		/// Creates a <see cref="UnaryExpression"/>.
		/// </summary>
		/// <param name="expression"><see cref="UnaryExpression"/> to visit.</param>
		/// <returns>Returns a <see cref="UnaryExpression"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitUnaryExpression"/>
		protected override object VisitUnaryExpression(UnaryExpression expression)
		{
			return Expression.MakeUnary(expression.NodeType, VisitExpression(expression.Operand), expression.Type, expression.Method);
		}

		/// <summary>
		/// Creates a <see cref="ElementInit"/>.
		/// </summary>
		/// <param name="elementInit"><see cref="ElementInit"/> to visit.</param>
		/// <returns>Returns a <see cref="ElementInit"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitElementInit"/>
		protected override object VisitElementInit(ElementInit elementInit)
		{
			return Expression.ElementInit(elementInit.AddMethod, VisitCollection<Expression>(elementInit.Arguments));
		}

		/// <summary>
		/// Creates a <see cref="MemberAssignment"/>.
		/// </summary>
		/// <param name="memberAssignment"><see cref="MemberAssignment"/> to visit.</param>
		/// <returns>Returns a <see cref="MemberAssignment"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberAssignment"/>
		protected override object VisitMemberAssignment(MemberAssignment memberAssignment)
		{
			return Expression.Bind(memberAssignment.Member, VisitExpression(memberAssignment.Expression));
		}

		/// <summary>
		/// Creates a <see cref="MemberListBinding"/>.
		/// </summary>
		/// <param name="memberListBinding"><see cref="MemberListBinding"/> to visit.</param>
		/// <returns>Returns a <see cref="MemberListBinding"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberListBinding"/>
		protected override object VisitMemberListBinding(MemberListBinding memberListBinding)
		{
			return Expression.ListBind(memberListBinding.Member, VisitObjectCollection<ElementInit>(memberListBinding.Initializers));
		}

		/// <summary>
		/// Creates a <see cref="MemberMemberBinding"/>.
		/// </summary>
		/// <param name="memberMemberBinding"><see cref="MemberMemberBinding"/> to visit.</param>
		/// <returns>Returns a <see cref="MemberMemberBinding"/>.</returns>
		/// <seealso cref="ExpressionVisitor.VisitMemberMemberBinding"/>
		protected override object VisitMemberMemberBinding(MemberMemberBinding memberMemberBinding)
		{
			return Expression.MemberBind(memberMemberBinding.Member, VisitObjectCollection<MemberBinding>(memberMemberBinding.Bindings));
		}

		#endregion
	}
}

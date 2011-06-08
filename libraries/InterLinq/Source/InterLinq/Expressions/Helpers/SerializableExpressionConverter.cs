using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security;
using InterLinq.Expressions.SerializableTypes;

namespace InterLinq.Expressions.Helpers
{
    /// <summary>
    /// Converter class to convert <see cref="SerializableExpression">SerializableExpression's</see>
    /// to <see cref="Expression">Expression's</see>.
    /// </summary>
    public class SerializableExpressionConverter : SerializableExpressionVisitor
    {

        #region Properties

        /// <summary>
        /// Gets the <see cref="IQueryHandler">QueryHandler</see>.
        /// </summary>
        public IQueryHandler QueryHandler { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes this class.
        /// </summary>
        /// <param name="expression"><see cref="SerializableExpression"/> to convert.</param>
        /// <param name="queryHandler"><see cref="IQueryHandler"/>.</param>
        public SerializableExpressionConverter(SerializableExpression expression, IQueryHandler queryHandler)
            : base(expression)
        {
            if (queryHandler == null)
            {
                throw new ArgumentNullException("queryHandler");
            }
            QueryHandler = queryHandler;
        }

        #endregion

        #region Visitor

        #region Visit serializable expression

        /// <summary>
        /// Visits a <see cref="SerializableBinaryExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableBinaryExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableBinaryExpression(SerializableBinaryExpression expression)
        {
            Expression left = Visit(expression.Left);
            Expression right = Visit(expression.Right);
            LambdaExpression conversion = (LambdaExpression)Visit(expression.Conversion);
            MethodInfo method = expression.Method != null ? (MethodInfo)expression.Method.GetClrVersion() : null;
            return Expression.MakeBinary(expression.NodeType, left, right, expression.IsLiftedToNull, method, conversion);
        }

        /// <summary>
        /// Visits a <see cref="SerializableConditionalExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableConditionalExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableConditionalExpression(SerializableConditionalExpression expression)
        {
            return Expression.Condition(Visit(expression.Test), Visit(expression.IfTrue), Visit(expression.IfFalse));
        }

        /// <summary>
        /// Visits a <see cref="SerializableConstantExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableConstantExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableConstantExpression(SerializableConstantExpression expression)
        {
            if (expression.Value != null)
            {
                return Expression.Constant(expression.Value, expression.Type.RepresentedType);
            }
            return Expression.Constant(null, (Type)expression.Type.GetClrVersion());
        }

        /// <summary>
        /// Visits a <see cref="SerializableInvocationExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableInvocationExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableInvocationExpression(SerializableInvocationExpression expression)
        {
            return Expression.Invoke(Visit(expression.Expression), VisitCollection<Expression>(expression.Arguments).ToArray());
        }

        /// <summary>
        /// Visits a <see cref="SerializableExpressionTyped"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableExpressionTyped"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableExpressionTyped<T>(SerializableExpressionTyped expression)
        {
            Expression body = Visit(expression.Body);
            IEnumerable<ParameterExpression> parameters = VisitCollection<ParameterExpression>(expression.Parameters);
            return Expression.Lambda<T>(body, parameters);
        }

        /// <summary>
        /// Visits a <see cref="SerializableLambdaExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableLambdaExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableLambdaExpression(SerializableLambdaExpression expression)
        {
            return Expression.Lambda(Visit(expression.Body), VisitCollection<ParameterExpression>(expression.Parameters).ToArray());
        }

        /// <summary>
        /// Visits a <see cref="SerializableListInitExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableListInitExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableListInitExpression(SerializableListInitExpression expression)
        {
            return Expression.ListInit(Visit(expression.NewExpression) as NewExpression, VisitObjectCollection<ElementInit>(expression.Initializers));
        }

        /// <summary>
        /// Visits a <see cref="SerializableMemberExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableMemberExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableMemberExpression(SerializableMemberExpression expression)
        {
            return Expression.MakeMemberAccess(Visit(expression.Expression), expression.Member.GetClrVersion());
        }

        /// <summary>
        /// Visits a <see cref="SerializableMemberInitExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableMemberInitExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableMemberInitExpression(SerializableMemberInitExpression expression)
        {
            IEnumerable<MemberBinding> bindings = VisitObjectCollection<MemberBinding>(expression.Bindings);
            return Expression.MemberInit(Visit(expression.NewExpression) as NewExpression, bindings);
        }

        /// <summary>
        /// Visits a <see cref="SerializableMethodCallExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableMethodCallExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableMethodCallExpression(SerializableMethodCallExpression expression)
        {
            return Expression.Call(Visit(expression.Object), (MethodInfo)expression.Method.GetClrVersion(), VisitCollection<Expression>(expression.Arguments));
        }

        /// <summary>
        /// Visits a <see cref="SerializableNewArrayExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableNewArrayExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableNewArrayExpression(SerializableNewArrayExpression expression)
        {
            if (expression.NodeType == ExpressionType.NewArrayBounds)
            {
                return Expression.NewArrayBounds((Type)expression.Type.GetClrVersion(), VisitCollection<Expression>(expression.Expressions));
            }
            Type t = (Type)expression.Type.GetClrVersion();
            // Expression must be an Array
            Debug.Assert(t.HasElementType);

            return Expression.NewArrayInit(t.GetElementType(), VisitCollection<Expression>(expression.Expressions));
        }

        /// <summary>
        /// Visits a <see cref="SerializableNewExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableNewExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableNewExpression(SerializableNewExpression expression)
        {
            if (expression.Members == null)
            {
                return Expression.New((ConstructorInfo)expression.Constructor.GetClrVersion(), VisitCollection<Expression>(expression.Arguments));
            }
            return Expression.New((ConstructorInfo)expression.Constructor.GetClrVersion(), VisitCollection<Expression>(expression.Arguments), expression.Members.Select(m => m.GetClrVersion()));
        }

        /// <summary>
        /// Visits a <see cref="SerializableParameterExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableParameterExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableParameterExpression(SerializableParameterExpression expression)
        {
            return Expression.Parameter((Type)expression.Type.GetClrVersion(), expression.Name);
        }

        /// <summary>
        /// Visits a <see cref="SerializableTypeBinaryExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableTypeBinaryExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableTypeBinaryExpression(SerializableTypeBinaryExpression expression)
        {
            return Expression.TypeIs(Visit(expression.Expression), (Type)expression.TypeOperand.GetClrVersion());
        }

        /// <summary>
        /// Visits a <see cref="SerializableUnaryExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableUnaryExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitSerializableUnaryExpression(SerializableUnaryExpression expression)
        {
            Expression operand = Visit(expression.Operand);
            Type type = (Type)expression.Type.GetClrVersion();
            MethodInfo method = expression.Method != null ? (MethodInfo)expression.Method.GetClrVersion() : null;
            return Expression.MakeUnary(expression.NodeType, operand, type, method);
        }

        /// <summary>
        /// Visits a <see cref="SerializableExpression"/>.
        /// </summary>
        /// <param name="expression"><see cref="SerializableExpression"/> to visit.</param>
        /// <returns>Returns the converted <see cref="Expression"/>.</returns>
        protected override Expression VisitUnknownSerializableExpression(SerializableExpression expression)
        {
            throw new Exception(string.Format("Expression \"{0}\" could not be handled.", expression));
        }

        #endregion

        #region Visit object

        /// <summary>
        /// Visits a <see cref="SerializableElementInit"/>.
        /// </summary>
        /// <param name="elementInit"><see cref="SerializableElementInit"/> to visit.</param>
        /// <returns>Returns the converted <see langword="object"/>.</returns>
        protected override object VisitSerializableElementInit(SerializableElementInit elementInit)
        {
            return Expression.ElementInit(elementInit.AddMethod, VisitCollection<Expression>(elementInit.Arguments).ToArray());
        }

        /// <summary>
        /// Visits a <see cref="SerializableMemberAssignment"/>.
        /// </summary>
        /// <param name="memberAssignment"><see cref="SerializableMemberAssignment"/> to visit.</param>
        /// <returns>Returns the converted <see langword="object"/>.</returns>
        protected override object VisitSerializableMemberAssignment(SerializableMemberAssignment memberAssignment)
        {
            return Expression.Bind(memberAssignment.Member, Visit(memberAssignment.Expression));
        }

        /// <summary>
        /// Visits a <see cref="SerializableMemberListBinding"/>.
        /// </summary>
        /// <param name="memberListBinding"><see cref="SerializableMemberListBinding"/> to visit.</param>
        /// <returns>Returns the converted <see langword="object"/>.</returns>
        protected override object VisitSerializableMemberListBinding(SerializableMemberListBinding memberListBinding)
        {
            return Expression.ListBind(memberListBinding.Member, VisitObjectCollection<ElementInit>(memberListBinding.Initializers));
        }

        /// <summary>
        /// Visits a <see cref="SerializableMemberMemberBinding"/>.
        /// </summary>
        /// <param name="memberMemberBinding"><see cref="SerializableMemberMemberBinding"/> to visit.</param>
        /// <returns>Returns the converted <see langword="object"/>.</returns>
        protected override object VisitSerializableMemberMemberBinding(SerializableMemberMemberBinding memberMemberBinding)
        {
            return Expression.MemberBind(memberMemberBinding.Member, VisitObjectCollection<MemberBinding>(memberMemberBinding.Bindings));
        }

        #endregion

        #region Get result

        /// <summary>
        /// Executes a <see cref="SerializableConstantExpression"/> and returns the result.
        /// </summary>
        /// <param name="expression"><see cref="SerializableConstantExpression"/> to convert.</param>
        /// <returns>Returns the result of a <see cref="SerializableConstantExpression"/>.</returns>
        protected override object GetResultConstantExpression(SerializableConstantExpression expression)
        {
            if (expression.Value == null)
            {
                return null;
            }
            if (expression.Value is InterLinqQueryBase)
            {
                Type type = ((InterLinqQueryBase)expression.Value).ElementType;
                return QueryHandler.Get(type);
            }
            return expression.Value;
        }

        /// <summary>
        /// Executes a <see cref="SerializableMethodCallExpression"/> and returns the result.
        /// </summary>
        /// <param name="expression"><see cref="SerializableMethodCallExpression"/> to convert.</param>
        /// <returns>Returns the result of a <see cref="SerializableMethodCallExpression"/>.</returns>
        protected override object GetResultMethodCallExpression(SerializableMethodCallExpression expression)
        {
            return InvokeMethodCall(expression);
        }

        #endregion

        #endregion

        /// <summary>
        /// Returns the return value of the method call in <paramref name="ex"/>.
        /// </summary>
        /// <param name="ex"><see cref="SerializableMethodCallExpression"/> to invoke.</param>
        /// <returns>Returns the return value of the method call in <paramref name="ex"/>.</returns>
        protected object InvokeMethodCall(SerializableMethodCallExpression ex)
        {
            if (ex.Method.DeclaringType.GetClrVersion() == typeof(Queryable))
            {
                List<object> args = new List<object>();
                Type[] parameterTypes = ex.Method.ParameterTypes.Select(p => (Type)p.GetClrVersion()).ToArray();
                for (int i = 0; i < ex.Arguments.Count && i < parameterTypes.Length; i++)
                {
                    SerializableExpression currentArg = ex.Arguments[i];
                    Type currentParameterType = parameterTypes[i];
                    if (typeof(Expression).IsAssignableFrom(currentParameterType))
                    {
                        args.Add(((UnaryExpression)Visit(currentArg)).Operand);
                    }
                    else
                    {
                        args.Add(VisitResult(currentArg));
                    }
                }
                return ((MethodInfo)ex.Method.GetClrVersion()).Invoke(ex.Object, args.ToArray());
            }

            // If the method is not of DeclaringType "Queryable", it mustn't be invoked.
            // Without this check, we were able to delete files from the server disk
            // using System.IO.File.Delete( ... )!
            throw new SecurityException(string.Format("Could not call method '{0}' of type '{1}'. Type must be Queryable.", ex.Method.Name, ex.Method.DeclaringType.Name));
        }
    }
}

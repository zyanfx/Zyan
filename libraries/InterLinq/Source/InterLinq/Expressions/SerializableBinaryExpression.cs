using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using InterLinq.Expressions.Helpers;
using InterLinq.Types;

namespace InterLinq.Expressions
{

    /// <summary>
    /// A serializable version of <see cref="BinaryExpression"/>.
    /// </summary>
    [Serializable]
    [DataContract]
    public class SerializableBinaryExpression : SerializableExpression
    {

        #region Constructors

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public SerializableBinaryExpression() { }

        /// <summary>
        /// Constructor with an <see cref="BinaryExpression"/> and an <see cref="ExpressionConverter"/>.
        /// </summary>
        /// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
        /// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
        public SerializableBinaryExpression(BinaryExpression expression, ExpressionConverter expConverter)
            : base(expression, expConverter)
        {
            Left = expression.Left.MakeSerializable(expConverter);
            Right = expression.Right.MakeSerializable(expConverter);
            Conversion = expression.Conversion.MakeSerializable<SerializableLambdaExpression>(expConverter);
            IsLiftedToNull = expression.IsLiftedToNull;
            Method = InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqMethodInfo>(expression.Method);
        }

        #endregion

        #region Properties

        /// <summary>
        /// See <see cref="BinaryExpression.Left"/>
        /// </summary>
        [DataMember]
        public SerializableExpression Left { get; set; }

        /// <summary>
        /// See <see cref="BinaryExpression.Right"/>
        /// </summary>
        [DataMember]
        public SerializableExpression Right { get; set; }

        /// <summary>
        /// See <see cref="BinaryExpression.Conversion"/>
        /// </summary>
        [DataMember]
        public SerializableLambdaExpression Conversion { get; set; }

        /// <summary>
        /// See <see cref="BinaryExpression.Method"/>
        /// </summary>
        [DataMember]
        public InterLinqMethodInfo Method { get; set; }

        /// <summary>
        /// See <see cref="BinaryExpression.IsLiftedToNull"/>
        /// </summary>
        [DataMember]
        public bool IsLiftedToNull { get; set; }

        #endregion

        #region ToString() Methods

        /// <summary>
        /// Builds a <see langword="string"/> representing the <see cref="Expression"/>.
        /// </summary>
        /// <param name="builder">A <see cref="System.Text.StringBuilder"/> to add the created <see langword="string"/>.</param>
        internal override void BuildString(StringBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (NodeType == ExpressionType.ArrayIndex)
            {
                Left.BuildString(builder);
                builder.Append("[");
                Right.BuildString(builder);
                builder.Append("]");
            }
            else
            {
                string @operator = GetOperator();
                if (@operator != null)
                {
                    builder.Append("(");
                    Left.BuildString(builder);
                    builder.Append(" ");
                    builder.Append(@operator);
                    builder.Append(" ");
                    Right.BuildString(builder);
                    builder.Append(")");
                }
                else
                {
                    builder.Append(NodeType);
                    builder.Append("(");
                    Left.BuildString(builder);
                    builder.Append(", ");
                    Right.BuildString(builder);
                    builder.Append(")");
                }
            }
        }

        /// <summary>
        /// Gets the operator as string.
        /// </summary>
        /// <returns>Returns the operator as a string.</returns>
        private string GetOperator()
        {
            switch (NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return "+";
                case ExpressionType.And:
                    if ((Type != InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(typeof(bool))) && (Type != InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(typeof(bool?))))
                    {
                        return "&";
                    }
                    return "And";
                case ExpressionType.AndAlso:
                    return "&&";
                case ExpressionType.Coalesce:
                    return "??";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.ExclusiveOr:
                    return "^";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LeftShift:
                    return "<<";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return "*";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.Or:
                    if ((Type != InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(typeof(bool))) && (Type != InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(typeof(bool?))))
                    {
                        return "|";
                    }
                    return "Or";
                case ExpressionType.OrElse:
                    return "||";
                case ExpressionType.Power:
                    return "^";
                case ExpressionType.RightShift:
                    return ">>";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return "-";
            }
            return null;
        }

        #endregion
    }
}

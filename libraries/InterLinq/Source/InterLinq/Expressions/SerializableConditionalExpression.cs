using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using InterLinq.Expressions.Helpers;

namespace InterLinq.Expressions
{

    /// <summary>
    /// A serializable version of <see cref="ConditionalExpression"/>.
    /// </summary>
    [Serializable]
    [DataContract]
    public class SerializableConditionalExpression : SerializableExpression
    {

        #region Constructors

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public SerializableConditionalExpression() { }

        /// <summary>
        /// Constructor with an <see cref="ConditionalExpression"/> and an <see cref="ExpressionConverter"/>.
        /// </summary>
        /// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
        /// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
        public SerializableConditionalExpression(ConditionalExpression expression, ExpressionConverter expConverter)
            : base(expression, expConverter)
        {
            IfTrue = expression.IfTrue.MakeSerializable(expConverter);
            IfFalse = expression.IfFalse.MakeSerializable(expConverter);
            Test = expression.Test.MakeSerializable(expConverter);
        }

        #endregion

        #region Properties

        /// <summary>
        /// See <see cref="ConditionalExpression.IfTrue"/>
        /// </summary>
        [DataMember]
        public SerializableExpression IfTrue { get; set; }

        /// <summary>
        /// See <see cref="ConditionalExpression.IfFalse"/>
        /// </summary>
        [DataMember]
        public SerializableExpression IfFalse { get; set; }

        /// <summary>
        /// See <see cref="ConditionalExpression.Test"/>
        /// </summary>
        [DataMember]
        public SerializableExpression Test { get; set; }

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
            builder.Append("IIF(");
            Test.BuildString(builder);
            builder.Append(", ");
            IfTrue.BuildString(builder);
            builder.Append(", ");
            IfFalse.BuildString(builder);
            builder.Append(")");
        }

        #endregion

    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using InterLinq.Expressions.Helpers;

namespace InterLinq.Expressions
{

    /// <summary>
    /// A serializable version of <see cref="LambdaExpression"/>.
    /// </summary>
    [Serializable]
    [DataContract]
    public class SerializableLambdaExpression : SerializableExpression
    {

        #region Constructors

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public SerializableLambdaExpression() { }

        /// <summary>
        /// Constructor with an <see cref="LambdaExpression"/> and an <see cref="ExpressionConverter"/>.
        /// </summary>
        /// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
        /// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
        public SerializableLambdaExpression(LambdaExpression expression, ExpressionConverter expConverter)
            : base(expression, expConverter)
        {
            Body = expression.Body.MakeSerializable(expConverter);
            Parameters = expression.Parameters.MakeSerializableCollection<SerializableParameterExpression>(expConverter);
        }

        #endregion

        #region Properties

        /// <summary>
        /// See <see cref="LambdaExpression.Body"/>
        /// </summary>
        [DataMember]
        public SerializableExpression Body { get; set; }

        /// <summary>
        /// See <see cref="LambdaExpression.Parameters"/>
        /// </summary>
        [DataMember]
        public ReadOnlyCollection<SerializableParameterExpression> Parameters { get; set; }

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
            if (Parameters.Count == 1)
            {
                Parameters[0].BuildString(builder);
            }
            else
            {
                builder.Append("(");
                int num = 0;
                int count = Parameters.Count;
                while (num < count)
                {
                    if (num > 0)
                    {
                        builder.Append(", ");
                    }
                    Parameters[num].BuildString(builder);
                    num++;
                }
                builder.Append(")");
            }
            builder.Append(" => ");
            Body.BuildString(builder);
        }

        #endregion

    }
}

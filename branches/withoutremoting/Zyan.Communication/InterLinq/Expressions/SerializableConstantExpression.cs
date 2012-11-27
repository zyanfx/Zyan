using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using Zyan.InterLinq.Expressions.Helpers;

namespace Zyan.InterLinq.Expressions
{
	/// <summary>
	/// A serializable version of <see cref="ConstantExpression"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableConstantExpression : SerializableExpression
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableConstantExpression() { }

		/// <summary>
		/// Constructor with an <see cref="ConstantExpression"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableConstantExpression(ConstantExpression expression, ExpressionConverter expConverter)
			: base(expression, expConverter)
		{
			if (expression.Value != null &&
				expression.Value is InterLinqQueryBase)
			{
				// Handle normal Query<> object
				Value = expression.Value;
			}
			else
			{
				// Compile variable into expression
				LambdaExpression lambda = Expression.Lambda(expression);
				Delegate fn = lambda.Compile();
				Value = Expression.Constant(fn.DynamicInvoke(null), expression.Type).Value;
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="ConstantExpression.Value"/>
		/// </summary>
		[DataMember]
		public object Value { get; set; }

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
			if (Value != null)
			{
				if (Value is string)
				{
					builder.Append("\"");
					builder.Append(Value);
					builder.Append("\"");
				}
				else if (Value.ToString() == Value.GetType().ToString())
				{
					builder.Append("value(");
					builder.Append(Value);
					builder.Append(")");
				}
				else
				{
					builder.Append(Value);
				}
			}
			else
			{
				builder.Append("null");
			}
		}

		#endregion
	}
}

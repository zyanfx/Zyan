using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using InterLinq.Expressions.Helpers;
using InterLinq.Types;

namespace InterLinq.Expressions
{
	/// <summary>
	/// A serializable version of <see cref="TypeBinaryExpression"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableTypeBinaryExpression : SerializableExpression
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableTypeBinaryExpression() { }

		/// <summary>
		/// Constructor with an <see cref="TypeBinaryExpression"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableTypeBinaryExpression(TypeBinaryExpression expression, ExpressionConverter expConverter)
			: base(expression, expConverter)
		{
			Expression = expression.Expression.MakeSerializable(expConverter);
			TypeOperand = InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(expression.TypeOperand);
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="TypeBinaryExpression.Expression"/>
		/// </summary>
		[DataMember]
		public SerializableExpression Expression { get; set; }

		/// <summary>
		/// See <see cref="TypeBinaryExpression.TypeOperand"/>
		/// </summary>
		[DataMember]
		public InterLinqType TypeOperand { get; set; }

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
			builder.Append("(");
			Expression.BuildString(builder);
			builder.Append(" Is ");
			builder.Append(TypeOperand.Name);
			builder.Append(")");
		}

		#endregion
	}
}

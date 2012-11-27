using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using Zyan.InterLinq.Expressions.Helpers;
using Zyan.InterLinq.Types;

namespace Zyan.InterLinq.Expressions
{
	/// <summary>
	/// A serializable version of <see cref="UnaryExpression"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableUnaryExpression : SerializableExpression
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableUnaryExpression() { }

		/// <summary>
		/// Constructor with an <see cref="UnaryExpression"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableUnaryExpression(UnaryExpression expression, ExpressionConverter expConverter)
			: base(expression, expConverter)
		{
			Operand = expression.Operand.MakeSerializable(expConverter);
			Method = InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqMethodInfo>(expression.Method);
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="UnaryExpression.Operand"/>
		/// </summary>
		[DataMember]
		public SerializableExpression Operand { get; set; }

		/// <summary>
		/// See <see cref="UnaryExpression.Method"/>
		/// </summary>
		[DataMember]
		public InterLinqMethodInfo Method { get; set; }

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
			switch (NodeType)
			{
				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
					builder.Append("-");
					Operand.BuildString(builder);
					return;

				case ExpressionType.UnaryPlus:
					builder.Append("+");
					Operand.BuildString(builder);
					return;

				case ExpressionType.Not:
					builder.Append("Not");
					builder.Append("(");
					Operand.BuildString(builder);
					builder.Append(")");
					return;

				case ExpressionType.Quote:
					Operand.BuildString(builder);
					return;

				case ExpressionType.TypeAs:
					builder.Append("(");
					Operand.BuildString(builder);
					builder.Append(" As ");
					builder.Append(Type.Name);
					builder.Append(")");
					return;
			}
			builder.Append(NodeType);
			builder.Append("(");
			Operand.BuildString(builder);
			builder.Append(")");
		}

		#endregion
	}
}

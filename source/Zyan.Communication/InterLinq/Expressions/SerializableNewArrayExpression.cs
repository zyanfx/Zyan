using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using InterLinq.Expressions.Helpers;

namespace InterLinq.Expressions
{
	/// <summary>
	/// A serializable version of <see cref="NewArrayExpression"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableNewArrayExpression : SerializableExpression
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableNewArrayExpression() { }

		/// <summary>
		/// Constructor with an <see cref="NewArrayExpression"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableNewArrayExpression(NewArrayExpression expression, ExpressionConverter expConverter)
			: base(expression, expConverter)
		{
			Expressions = expression.Expressions.MakeSerializableCollection<SerializableExpression>(expConverter);
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="NewArrayExpression.Expressions"/>
		/// </summary>
		[DataMember]
		public ReadOnlyCollection<SerializableExpression> Expressions { get; set; }

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
				case ExpressionType.NewArrayInit:
					builder.Append("new ");
					builder.Append("[] {");
					int num3 = 0;
					int count = Expressions.Count;
					while (num3 < count)
					{
						if (num3 > 0)
						{
							builder.Append(", ");
						}
						Expressions[num3].BuildString(builder);
						num3++;
					}
					builder.Append("}");
					return;
				case ExpressionType.NewArrayBounds:
					builder.Append("new ");
					builder.Append(Type.ToString());
					builder.Append("(");
					int num = 0;
					int num2 = Expressions.Count;
					while (num < num2)
					{
						if (num > 0)
						{
							builder.Append(", ");
						}
						Expressions[num].BuildString(builder);
						num++;
					}
					builder.Append(")");
					return;
			}
		}

		#endregion
	}
}

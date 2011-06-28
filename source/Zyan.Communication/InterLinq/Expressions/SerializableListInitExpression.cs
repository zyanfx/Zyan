using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Zyan.InterLinq.Expressions.Helpers;
using Zyan.InterLinq.Expressions.SerializableTypes;
using System.Text;

namespace Zyan.InterLinq.Expressions
{
	/// <summary>
	/// A serializable version of <see cref="ListInitExpression"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableListInitExpression : SerializableExpression
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableListInitExpression() { }

		/// <summary>
		/// Constructor with an <see cref="ListInitExpression"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableListInitExpression(ListInitExpression expression, ExpressionConverter expConverter)
			: base(expression, expConverter)
		{
			NewExpression = expression.NewExpression.MakeSerializable<SerializableNewExpression>(expConverter);
			Initializers = expConverter.ConvertToSerializableObjectCollection<SerializableElementInit>(expression.Initializers);
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="ListInitExpression.NewExpression"/>
		/// </summary>
		[DataMember]
		public SerializableNewExpression NewExpression { get; set; }

		/// <summary>
		/// See <see cref="ListInitExpression.Initializers"/>
		/// </summary>
		[DataMember]
		public ReadOnlyCollection<SerializableElementInit> Initializers { get; set; }

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
			NewExpression.BuildString(builder);
			builder.Append(" {");
			int num = 0;
			int count = Initializers.Count;
			while (num < count)
			{
				if (num > 0)
				{
					builder.Append(", ");
				}
				Initializers[num].BuildString(builder);
				num++;
			}
			builder.Append("}");
		}

		#endregion
	}
}

using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using Zyan.InterLinq.Expressions.Helpers;

namespace Zyan.InterLinq.Expressions
{
	/// <summary>
	/// A serializable version of <see cref="InvocationExpression"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableInvocationExpression : SerializableExpression
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableInvocationExpression() { }

		/// <summary>
		/// Constructor with an <see cref="InvocationExpression"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableInvocationExpression(InvocationExpression expression, ExpressionConverter expConverter)
			: base(expression, expConverter)
		{
			Expression = expression.Expression.MakeSerializable(expConverter);
			Arguments = expression.Arguments.MakeSerializableCollection<SerializableExpression>(expConverter);
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="InvocationExpression.Expression"/>
		/// </summary>
		[DataMember]
		public SerializableExpression Expression { get; set; }

		/// <summary>
		/// See <see cref="InvocationExpression.Arguments"/>
		/// </summary>
		[DataMember]
		public ReadOnlyCollection<SerializableExpression> Arguments { get; set; }

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
			builder.Append("Invoke(");
			Expression.BuildString(builder);
			int num = 0;
			int count = Arguments.Count;
			while (num < count)
			{
				builder.Append(",");
				Arguments[num].BuildString(builder);
				num++;
			}
			builder.Append(")");
		}

		#endregion
	}
}

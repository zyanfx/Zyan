using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using InterLinq.Expressions.Helpers;

namespace InterLinq.Expressions
{
	/// <summary>
	/// A serializable version of <see cref="ParameterExpression"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableParameterExpression : SerializableExpression
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableParameterExpression() { }

		/// <summary>
		/// Constructor with an <see cref="ParameterExpression"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableParameterExpression(ParameterExpression expression, ExpressionConverter expConverter)
			: base(expression, expConverter)
		{
			Name = expression.Name;
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="ParameterExpression.Name"/>
		/// </summary>
		[DataMember]
		public string Name { get; set; }

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
			if (Name != null)
			{
				builder.Append(Name);
			}
			else
			{
				builder.Append("<param>");
			}
		}

		#endregion
	}
}

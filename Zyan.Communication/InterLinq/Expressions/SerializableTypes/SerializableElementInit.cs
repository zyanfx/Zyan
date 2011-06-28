using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using InterLinq.Expressions.Helpers;
using System.Text;

namespace InterLinq.Expressions.SerializableTypes
{
	/// <summary>
	/// A serializable version of <see cref="ElementInit"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableElementInit
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableElementInit() { }

		/// <summary>
		/// Constructor with an <see cref="ElementInit"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="elementInit">The original, not serializable <see cref="ElementInit"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableElementInit(ElementInit elementInit, ExpressionConverter expConverter)
		{
			Arguments = elementInit.Arguments.MakeSerializableCollection<SerializableExpression>(expConverter);
			AddMethod = elementInit.AddMethod;
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="ElementInit.Arguments"/>
		/// </summary>
		[DataMember]
		public ReadOnlyCollection<SerializableExpression> Arguments { get; set; }

		/// <summary>
		/// See <see cref="ElementInit.AddMethod"/>
		/// </summary>
		[DataMember]
		public MethodInfo AddMethod { get; set; }

		#endregion

		#region ToString() Methods

		/// <summary>
		/// Builds a <see langword="string"/> representing the <see cref="ElementInit"/>.
		/// </summary>
		/// <param name="builder">A <see cref="System.Text.StringBuilder"/> to add the created <see langword="string"/>.</param>
		internal void BuildString(StringBuilder builder)
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
			}
			builder.Append(AddMethod);
			builder.Append("(");
			bool flag = true;
			foreach (SerializableExpression expression in Arguments)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					builder.Append(",");
				}
				expression.BuildString(builder);
			}
			builder.Append(")");
		}

		/// <summary>
		/// Returns a <see langword="string"/> representing the <see cref="ElementInit"/>.
		/// </summary>
		/// <returns>Returns a <see langword="string"/> representing this object.</returns>
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			BuildString(builder);
			return builder.ToString();
		}

		#endregion
	}
}

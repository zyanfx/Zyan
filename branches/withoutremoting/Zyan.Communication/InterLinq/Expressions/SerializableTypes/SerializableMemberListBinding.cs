using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using Zyan.InterLinq.Expressions.Helpers;

namespace Zyan.InterLinq.Expressions.SerializableTypes
{
	/// <summary>
	/// A serializable version of <see cref="MemberListBinding"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableMemberListBinding : SerializableMemberBinding
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableMemberListBinding() { }

		/// <summary>
		/// Constructor with an <see cref="MemberListBinding"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="memberListBinding">The original, not serializable <see cref="MemberBinding"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableMemberListBinding(MemberListBinding memberListBinding, ExpressionConverter expConverter)
			: base(memberListBinding, expConverter)
		{
			Initializers = expConverter.ConvertToSerializableObjectCollection<SerializableElementInit>(memberListBinding.Initializers);
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="MemberListBinding.Initializers"/>
		/// </summary>
		[DataMember]
		public ReadOnlyCollection<SerializableElementInit> Initializers { get; set; }

		#endregion

		#region ToString() Methods

		/// <summary>
		/// Builds a <see langword="string"/> representing the <see cref="MemberBinding"/>.
		/// </summary>
		/// <param name="builder">A <see cref="System.Text.StringBuilder"/> to add the created <see langword="string"/>.</param>
		internal override void BuildString(StringBuilder builder)
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
			}
			builder.Append(Member.Name);
			builder.Append(" = {");
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

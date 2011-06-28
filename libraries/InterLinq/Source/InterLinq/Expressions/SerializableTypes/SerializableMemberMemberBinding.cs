using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using InterLinq.Expressions.Helpers;

namespace InterLinq.Expressions.SerializableTypes
{
	/// <summary>
	/// A serializable version of <see cref="MemberMemberBinding"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableMemberMemberBinding : SerializableMemberBinding
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableMemberMemberBinding() { }

		/// <summary>
		/// Constructor with an <see cref="MemberMemberBinding"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="memberMemberBinding">The original, not serializable <see cref="MemberBinding"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableMemberMemberBinding(MemberMemberBinding memberMemberBinding, ExpressionConverter expConverter)
			: base(memberMemberBinding, expConverter)
		{
			Bindings = expConverter.ConvertToSerializableObjectCollection<SerializableMemberBinding>(memberMemberBinding.Bindings);
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="MemberMemberBinding.Bindings"/>
		/// </summary>
		[DataMember]
		public ReadOnlyCollection<SerializableMemberBinding> Bindings { get; set; }

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
			int count = Bindings.Count;
			while (num < count)
			{
				if (num > 0)
				{
					builder.Append(", ");
				}
				Bindings[num].BuildString(builder);
				num++;
			}
			builder.Append("}");
		}

		#endregion
	}
}

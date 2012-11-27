using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Zyan.InterLinq.Types
{
	/// <summary>
	/// InterLINQ representation of the nested <see cref="Type"/>.
	/// </summary>
	/// <seealso cref="InterLinqMemberInfo"/>
	/// <seealso cref="Type"/>
	[Serializable]
	[DataContract]
	public class InterLinqNestedType : InterLinqType
	{
		#region Properties

		/// <summary>
		/// Gets the <see cref="MemberTypes">MemberType</see>.
		/// </summary>
		/// <seealso cref="Type.MemberType"/>
		public override MemberTypes MemberType
		{
			get { return MemberTypes.NestedType; }
		}

		#endregion

		#region Constructors / Initialization

		/// <summary>
		/// Empty constructor.
		/// </summary>
		public InterLinqNestedType() : base()
		{
		}

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="representedType">Represented CLR <see cref="Type"/>.</param>
		public InterLinqNestedType(Type representedType) : this()
		{
			Initialize(representedType);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>A hash code for the current <see langword="object"/>.</returns>
		public override int GetHashCode()
		{
			int num = -339665780;
			num ^= EqualityComparer<bool>.Default.GetHashCode(IsGeneric);
			num ^= EqualityComparer<Type>.Default.GetHashCode(RepresentedType);
			GenericArguments.ForEach(o => num ^= EqualityComparer<InterLinqType>.Default.GetHashCode(o));
			return num ^ base.GetHashCode();
		}

		#endregion
	}
}

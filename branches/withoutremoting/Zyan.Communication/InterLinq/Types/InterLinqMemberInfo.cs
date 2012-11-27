using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Zyan.InterLinq.Types
{
	/// <summary>
	/// InterLINQ representation of <see cref="MemberInfo"/>.
	/// </summary>
	/// <seealso cref="MemberInfo"/>
	[Serializable]
	[DataContract]
	public abstract class InterLinqMemberInfo
	{
		#region Properties

		/// <summary>
		/// Gets the <see cref="MemberTypes">MemberType</see>.
		/// </summary>
		/// <seealso cref="MemberInfo.MemberType"/>
		public abstract MemberTypes MemberType { get; }

		/// <summary>
		/// Gets or sets the name of this <see cref="InterLinqMemberInfo"/>.
		/// </summary>
		/// <seealso cref="MemberInfo.Name"/>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the class that declares this <see cref="InterLinqMemberInfo"/>.
		/// </summary>
		/// <seealso cref="MemberInfo.DeclaringType"/>
		[DataMember]
		public InterLinqType DeclaringType { get; set; }

		#endregion

		#region Constructors / Initialization

		/// <summary>
		/// Empty constructor.
		/// </summary>
		protected InterLinqMemberInfo() { }

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="memberInfo">Represented CLR <see cref="MemberInfo"/>.</param>
		protected InterLinqMemberInfo(MemberInfo memberInfo)
		{
			Initialize(memberInfo);
		}

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="memberInfo">Represented <see cref="MemberInfo"/></param>
		public virtual void Initialize(MemberInfo memberInfo)
		{
			Name = memberInfo.Name;
			DeclaringType = InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(memberInfo.DeclaringType);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns the CLR <see cref="MemberInfo"/>.
		/// </summary>
		/// <returns>Returns the CLR <see cref="MemberInfo"/>.</returns>
		public abstract MemberInfo GetClrVersion();

		/// <summary>
		/// Compares <paramref name="obj"/> to this instance.
		/// </summary>
		/// <param name="obj"><see langword="object"/> to compare.</param>
		/// <returns>True if equal, false if not.</returns>
		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}
			InterLinqMemberInfo other = (InterLinqMemberInfo)obj;
			return MemberType == other.MemberType && Name == other.Name && DeclaringType.Equals(other.DeclaringType);
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>A hash code for the current <see langword="object"/>.</returns>
		public override int GetHashCode()
		{
			int num = -657106008;
			num ^= EqualityComparer<string>.Default.GetHashCode(Name);
			num ^= EqualityComparer<InterLinqType>.Default.GetHashCode(DeclaringType);
			return num;
		}

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Zyan.InterLinq.Types
{
	/// <summary>
	/// InterLINQ representation of <see cref="FieldInfo"/>.
	/// </summary>
	/// <seealso cref="InterLinqMemberInfo"/>
	/// <seealso cref="FieldInfo"/>
	[Serializable]
	[DataContract]
	public class InterLinqFieldInfo : InterLinqMemberInfo
	{
		#region Properties

		/// <summary>
		/// Gets the <see cref="MemberTypes">MemberType</see>.
		/// </summary>
		/// <seealso cref="FieldInfo.MemberType"/>
		public override MemberTypes MemberType
		{
			get { return MemberTypes.Field; }
		}

		/// <summary>
		/// Gets or sets the <see cref="InterLinqType"/> of this field.
		/// </summary>
		/// <seealso cref="FieldInfo.FieldType"/>
		[DataMember]
		public InterLinqType FieldType { get; set; }

		#endregion

		#region Constructors / Initialization

		/// <summary>
		/// Empty constructor.
		/// </summary>
		public InterLinqFieldInfo() { }

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="fieldInfo">Represented CLR <see cref="FieldInfo"/>.</param>
		public InterLinqFieldInfo(FieldInfo fieldInfo)
		{
			Initialize(fieldInfo);
		}

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="memberInfo">Represented <see cref="MemberInfo"/></param>
		/// <seealso cref="InterLinqMemberInfo.Initialize"/>
		public override void Initialize(MemberInfo memberInfo)
		{
			base.Initialize(memberInfo);
			FieldInfo fieldInfo = memberInfo as FieldInfo;
			FieldType = InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(fieldInfo.FieldType);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns the CLR <see cref="MemberInfo"/>.
		/// </summary>
		/// <returns>Returns the CLR <see cref="MemberInfo"/>.</returns>
		public override MemberInfo GetClrVersion()
		{
			InterLinqTypeSystem tsInstance = InterLinqTypeSystem.Instance;
			lock (tsInstance)
			{
				if (tsInstance.IsInterLinqMemberInfoRegistered(this))
				{
					return tsInstance.GetClrVersion<FieldInfo>(this);
				}

				var declaringType = (Type)DeclaringType.GetClrVersion();
				var foundField = declaringType.GetField(Name);
				if (foundField == null)
				{
					foundField = declaringType.GetField(Name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				}

				tsInstance.SetClrVersion(this, foundField);
				return foundField;
			}
		}

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
			if (!base.Equals(obj))
			{
				return false;
			}
			InterLinqFieldInfo other = (InterLinqFieldInfo)obj;
			return FieldType.Equals(other.FieldType);
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>A hash code for the current <see langword="object"/>.</returns>
		public override int GetHashCode()
		{
			int num = 1708362606;
			num ^= EqualityComparer<InterLinqType>.Default.GetHashCode(FieldType);
			return num ^ base.GetHashCode();
		}

		#endregion
	}
}

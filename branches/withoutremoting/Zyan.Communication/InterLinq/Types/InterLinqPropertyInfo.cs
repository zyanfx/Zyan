using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Zyan.InterLinq.Types
{
	/// <summary>
	/// InterLINQ representation of <see cref="PropertyInfo"/>.
	/// </summary>
	/// <seealso cref="InterLinqMemberInfo"/>
	/// <seealso cref="PropertyInfo"/>
	[Serializable]
	[DataContract]
	public class InterLinqPropertyInfo : InterLinqMemberInfo
	{
		#region Properties

		/// <summary>
		/// Gets the <see cref="MemberTypes">MemberType</see>.
		/// </summary>
		/// <seealso cref="InterLinqMemberInfo.MemberType"/>
		/// <seealso cref="PropertyInfo.MemberType"/>
		public override MemberTypes MemberType
		{
			get { return MemberTypes.Property; }
		}

		/// <summary>
		/// Gets or sets the <see cref="InterLinqType"/> of this property.
		/// </summary>
		/// <seealso cref="PropertyInfo.PropertyType"/>
		[DataMember]
		public InterLinqType PropertyType { get; set; }

		#endregion

		#region Constructors / Initialization

		/// <summary>
		/// Empty constructor.
		/// </summary>
		public InterLinqPropertyInfo() { }

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="fieldInfo">Represented CLR <see cref="PropertyInfo"/>.</param>
		public InterLinqPropertyInfo(PropertyInfo fieldInfo)
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
			PropertyInfo propertyInfo = memberInfo as PropertyInfo;
			PropertyType = InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(propertyInfo.PropertyType);
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
					return tsInstance.GetClrVersion<PropertyInfo>(this);
				}

				Type declaringType = (Type)DeclaringType.GetClrVersion();
				PropertyInfo foundProperty = declaringType.GetProperty(Name);
				tsInstance.SetClrVersion(this, foundProperty);
				return foundProperty;
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
			InterLinqPropertyInfo other = (InterLinqPropertyInfo)obj;
			return PropertyType.Equals(other.PropertyType);
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>A hash code for the current <see langword="object"/>.</returns>
		public override int GetHashCode()
		{
			int num = -1141188190;
			num ^= EqualityComparer<InterLinqType>.Default.GetHashCode(PropertyType);
			return num ^ base.GetHashCode();
		}

		#endregion
	}
}

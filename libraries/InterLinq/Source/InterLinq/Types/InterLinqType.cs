using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace InterLinq.Types
{
	/// <summary>
	/// InterLINQ representation of <see cref="Type"/>.
	/// </summary>
	/// <seealso cref="InterLinqMemberInfo"/>
	/// <seealso cref="Type"/>
	[Serializable]
	[DataContract]
	public class InterLinqType : InterLinqMemberInfo
	{
		#region Properties

		/// <summary>
		/// Gets or sets if this is a generic <see cref="Type"/>.
		/// </summary>
		/// <seealso cref="Type.IsGenericType"/>
		[DataMember]
		public virtual bool IsGeneric { get; set; }

		/// <summary>
		/// Gets the <see cref="MemberTypes">MemberType</see>.
		/// </summary>
		/// <seealso cref="Type.MemberType"/>
		public override MemberTypes MemberType
		{
			get { return MemberTypes.TypeInfo; }
		}

		[DataMember(Name = "RepresentedType")]
		private String representedType;

		/// <summary>
		/// Gets or sets the represented <see cref="Type"/>.
		/// </summary> 
		public Type RepresentedType
		{
			get { return Type.GetType(representedType); }
			set { representedType = value.AssemblyQualifiedName; }
		}

		/// <summary>
		/// Gets or sets the generic Arguments.
		/// </summary>
		/// <seealso cref="Type.GetGenericArguments"/>
		[DataMember]
		public List<InterLinqType> GenericArguments { get; set; }

		#endregion

		#region Constructors / Initialization

		/// <summary>
		/// Empty constructor.
		/// </summary>
		public InterLinqType()
		{
			GenericArguments = new List<InterLinqType>();
		}

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="representedType">Represented CLR <see cref="Type"/>.</param>
		public InterLinqType(Type representedType) : this()
		{
			Initialize(representedType);
		}

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="memberInfo">Represented <see cref="MemberInfo"/></param>
		/// <seealso cref="InterLinqMemberInfo.Initialize"/>
		public override void Initialize(MemberInfo memberInfo)
		{
			Type repType = memberInfo as Type;
			Name = repType.Name;
			if (repType.IsGenericType)
			{
				RepresentedType = repType.GetGenericTypeDefinition();
				IsGeneric = true;
				foreach (Type genericArgument in repType.GetGenericArguments())
				{
					GenericArguments.Add(InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(genericArgument));
				}
			}
			else
			{
				RepresentedType = repType;
				IsGeneric = false;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Creates and returns the CLR <see cref="Type"/>.
		/// </summary>
		/// <returns>Creates and returns the CLR <see cref="Type"/>.</returns>
		protected virtual Type CreateClrType()
		{
			if (!IsGeneric)
			{
				return RepresentedType;
			}
			return RepresentedType.MakeGenericType(GenericArguments.Select(arg => (Type)arg.GetClrVersion()).ToArray());
		}

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
					return tsInstance.GetClrVersion<Type>(this);
				}
				Type createdType = CreateClrType();
				tsInstance.SetClrVersion(this, createdType);
				return createdType;
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
			InterLinqType other = (InterLinqType)obj;
			if (GenericArguments.Count != other.GenericArguments.Count)
			{
				return false;
			}
			for (int i = 0; i < GenericArguments.Count; i++)
			{
				if (!GenericArguments[i].Equals(other.GenericArguments[i]))
				{
					return false;
				}
			}
			return MemberType == other.MemberType && representedType == other.representedType && Name == other.Name && IsGeneric == other.IsGeneric;
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>A hash code for the current <see langword="object"/>.</returns>
		public override int GetHashCode()
		{
			int num = -657803396;
			num ^= EqualityComparer<bool>.Default.GetHashCode(IsGeneric);
			num ^= EqualityComparer<Type>.Default.GetHashCode(RepresentedType);
			GenericArguments.ForEach(o => num ^= EqualityComparer<InterLinqType>.Default.GetHashCode(o));
			return num ^ base.GetHashCode();
		}

		#endregion
	}
}

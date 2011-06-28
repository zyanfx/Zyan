using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace InterLinq.Types
{
	/// <summary>
	/// InterLINQ representation of <see cref="ConstructorInfo"/>.
	/// </summary>
	/// <seealso cref="InterLinqMethodBase"/>
	/// <seealso cref="InterLinqMemberInfo"/>
	/// <seealso cref="ConstructorInfo"/>
	[Serializable]
	[DataContract]
	public class InterLinqConstructorInfo : InterLinqMethodBase
	{
		#region Properties

		/// <summary>
		/// Gets the <see cref="MemberTypes">MemberType</see>.
		/// </summary>
		/// <seealso cref="InterLinqMemberInfo.MemberType"/>
		/// <seealso cref="ConstructorInfo.MemberType"/>
		public override MemberTypes MemberType
		{
			get { return MemberTypes.Constructor; }
		}

		#endregion

		#region Constructors / Initialization

		/// <summary>
		/// Empty constructor.
		/// </summary>
		public InterLinqConstructorInfo() { }

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="constrcutorInfo">Represented CLR <see cref="ConstructorInfo"/>.</param>
		public InterLinqConstructorInfo(ConstructorInfo constrcutorInfo)
		{
			Initialize(constrcutorInfo);
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
					return tsInstance.GetClrVersion<ConstructorInfo>(this);
				}

				Type declaringType = (Type)DeclaringType.GetClrVersion();
				ConstructorInfo foundConstructor = declaringType.GetConstructor(ParameterTypes.Select(p => (Type)p.GetClrVersion()).ToArray());
				tsInstance.SetClrVersion(this, foundConstructor);
				return foundConstructor;
			}
		}

		#endregion
	}
}

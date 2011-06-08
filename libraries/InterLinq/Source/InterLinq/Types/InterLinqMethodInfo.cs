using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace InterLinq.Types
{
    /// <summary>
    /// InterLINQ representation of <see cref="MethodInfo"/>.
    /// </summary>
    /// <seealso cref="InterLinqMethodBase"/>
    /// <seealso cref="InterLinqMemberInfo"/>
    /// <seealso cref="MethodInfo"/>
    [Serializable]
    [DataContract]
    public class InterLinqMethodInfo : InterLinqMethodBase
    {

        #region Properties

        /// <summary>
        /// Gets the <see cref="MemberTypes">MemberType</see>.
        /// </summary>
        /// <seealso cref="InterLinqMemberInfo.MemberType"/>
        /// <seealso cref="MethodInfo.MemberType"/>
        public override MemberTypes MemberType
        {
            get { return MemberTypes.Method; }
        }

        /// <summary>
        /// Gets or sets the <see cref="InterLinqType">ReturnType</see>.
        /// </summary>
        /// <seealso cref="MethodInfo.ReturnType"/>
        [DataMember]
        public InterLinqType ReturnType { get; set; }

        /// <summary>
        /// Returns true if the <see cref="InterLinqMethodInfo"/> is generic.
        /// </summary>
        /// <seealso cref="MethodInfo.IsGenericMethod"/>
        public bool IsGeneric
        {
            get { return GenericArguments.Count > 0; }
        }
        
        /// <summary>
        /// Gets or sets the generic arguments.
        /// </summary>
        /// <seealso cref="MethodInfo.GetGenericArguments"/>
        [DataMember]
        public List<InterLinqType> GenericArguments { get; set; }

        #endregion

        #region Constructors / Initialization

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public InterLinqMethodInfo()
        {
            GenericArguments = new List<InterLinqType>();
        }

        /// <summary>
        /// Initializes this class.
        /// </summary>
        /// <param name="methodInfo">Represented CLR <see cref="MethodInfo"/>.</param>
        public InterLinqMethodInfo(MethodInfo methodInfo)
        {
            GenericArguments = new List<InterLinqType>();
            Initialize(methodInfo);
        }

        /// <summary>
        /// Initializes this class.
        /// </summary>
        /// <param name="memberInfo">Represented <see cref="MemberInfo"/></param>
        /// <seealso cref="InterLinqMemberInfo.Initialize"/>
        public override void Initialize(MemberInfo memberInfo)
        {
            base.Initialize(memberInfo);
            MethodInfo methodInfo = memberInfo as MethodInfo;
            ReturnType = InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(methodInfo.ReturnType);
            if (methodInfo.IsGenericMethod)
            {
                foreach (Type genericArgument in methodInfo.GetGenericArguments())
                {
                    GenericArguments.Add(InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqType>(genericArgument));
                }
            }
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
                    return tsInstance.GetClrVersion<MethodInfo>(this);
                }

                Type declaringType = (Type)DeclaringType.GetClrVersion();
                Type[] genericArgumentTypes = GenericArguments.Select(p => (Type)p.GetClrVersion()).ToArray();
                MethodInfo foundMethod = null;
                foreach (MethodInfo method in declaringType.GetMethods().Where(m => m.Name == Name))
                {
                    MethodInfo currentMethod = method;
                    if (currentMethod.IsGenericMethod)
                    {
                        if (currentMethod.GetGenericArguments().Length == genericArgumentTypes.Length)
                        {
                            currentMethod = currentMethod.MakeGenericMethod(genericArgumentTypes);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    ParameterInfo[] currentParameters = currentMethod.GetParameters();
                    if (ParameterTypes.Count == currentParameters.Length)
                    {
                        bool allArumentsFit = true;
                        for (int i = 0; i < ParameterTypes.Count && i < currentParameters.Length; i++)
                        {
                            Type currentArg = (Type)ParameterTypes[i].GetClrVersion();
                            Type currentParamType = currentParameters[i].ParameterType;
                            if (!currentParamType.IsAssignableFrom(currentArg))
                            {
                                allArumentsFit = false;
                                break;
                            }
                        }
                        if (allArumentsFit)
                        {
                            foundMethod = currentMethod;
                        }
                    }
                }

                if (foundMethod == null)
                {
                    throw new Exception(string.Format("Method \"{0}.{1}\" not found.", declaringType, Name));
                }
                tsInstance.SetClrVersion(this, foundMethod);
                return foundMethod;
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
            InterLinqMethodInfo other = (InterLinqMethodInfo)obj;
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

            return ReturnType.Equals(other.ReturnType);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current <see langword="object"/>.</returns>
        public override int GetHashCode()
        {
            int num = 1302103589;
            num ^= EqualityComparer<InterLinqType>.Default.GetHashCode(ReturnType);
            GenericArguments.ForEach(o => num ^= EqualityComparer<InterLinqType>.Default.GetHashCode(o));
            return num ^ base.GetHashCode();
        }

        #endregion

    }
}

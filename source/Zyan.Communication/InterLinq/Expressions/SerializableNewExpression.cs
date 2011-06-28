using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using InterLinq.Expressions.Helpers;
using InterLinq.Types;

namespace InterLinq.Expressions
{
	/// <summary>
	/// A serializable version of <see cref="NewArrayExpression"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SerializableNewExpression : SerializableExpression
	{
		#region Constructors

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public SerializableNewExpression() { }

		/// <summary>
		/// Constructor with an <see cref="NewExpression"/> and an <see cref="ExpressionConverter"/>.
		/// </summary>
		/// <param name="expression">The original, not serializable <see cref="Expression"/>.</param>
		/// <param name="expConverter">The <see cref="ExpressionConverter"/> to convert contained <see cref="Expression">Expressions</see>.</param>
		public SerializableNewExpression(NewExpression expression, ExpressionConverter expConverter)
			: base(expression, expConverter)
		{
			Arguments = expression.Arguments.MakeSerializableCollection<SerializableExpression>(expConverter);
			Members = InterLinqTypeSystem.Instance.GetCollectionOf<InterLinqMemberInfo>(expression.Members);
			Constructor = InterLinqTypeSystem.Instance.GetInterLinqVersionOf<InterLinqConstructorInfo>(expression.Constructor);
		}

		#endregion

		#region Properties

		/// <summary>
		/// See <see cref="NewExpression.Arguments"/>
		/// </summary>
		[DataMember]
		public ReadOnlyCollection<SerializableExpression> Arguments { get; set; }

		/// <summary>
		/// See <see cref="NewExpression.Members"/>
		/// </summary>
		[DataMember]
		public ReadOnlyCollection<InterLinqMemberInfo> Members { get; set; }

		/// <summary>
		/// See <see cref="NewExpression.Constructor"/>
		/// </summary>
		[DataMember]
		public InterLinqConstructorInfo Constructor { get; set; }

		#endregion

		#region ToString() Methods

		internal override void BuildString(StringBuilder builder)
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
			}
			InterLinqType type = (Constructor == null) ? Type : Constructor.DeclaringType;
			builder.Append("new ");
			int count = Arguments.Count;
			builder.Append(type.Name);
			builder.Append("(");
			if (count > 0)
			{
				for (int i = 0; i < count; i++)
				{
					if (i > 0)
					{
						builder.Append(", ");
					}
					if (Members != null)
					{
						string propertyName;
						if ((Members[i].MemberType == MemberTypes.Method) && ((propertyName = GetPropertyNoThrow((InterLinqMethodInfo)Members[i])) != null))
						{
							builder.Append(propertyName);
						}
						else
						{
							builder.Append(Members[i].Name);
						}
						builder.Append(" = ");
					}
					Arguments[i].BuildString(builder);
				}
			}
			builder.Append(")");
		}

		private static string GetPropertyNoThrow(InterLinqMethodInfo method)
		{
			if (method != null)
			{
#warning Implement
				//InterLinqType declaringType = method.DeclaringType;
				//BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Public;
				//bindingAttr |= method.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
				//foreach( PropertyInfo info in declaringType.GetProperties( bindingAttr ) ) {
				//    if( info.CanRead && ( method == info.GetGetMethod( true ) ) ) {
				//        return info;
				//    }
				//    if( info.CanWrite && ( method == info.GetSetMethod( true ) ) ) {
				//        return info;
				//    }
				//}
			}
			return null;
		}

		#endregion
	}
}

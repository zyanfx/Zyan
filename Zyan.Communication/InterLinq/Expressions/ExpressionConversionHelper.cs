using System.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Zyan.InterLinq.Expressions.Helpers;
using Zyan.InterLinq.Communication;

namespace Zyan.InterLinq.Expressions
{
	/// <summary>
	/// Static helper class providing several C# 3.0 Extension Methods.
	/// </summary>
	/// <seealso cref="SerializableExpression"/>
	/// <seealso cref="Expression"/>
	/// <seealso cref="IEnumerable"/>
	public static class ExpressionConversionHelper
	{
		/// <summary>
		/// Extension Method for <see cref="Expression"/>.
		/// Converts an <see cref="Expression"/> to a <see cref="SerializableExpression"/>.
		/// </summary>
		/// <param name="exp">Extended class instance.</param>
		/// <returns>Returns the converted <see cref="SerializableExpression"/>.</returns>
		public static SerializableExpression MakeSerializable(this Expression exp)
		{
			return new ExpressionConverter(exp).Visit() as SerializableExpression;
		}

		/// <summary>
		/// Extension Method for <see cref="Expression"/>.
		/// Converts an <see cref="Expression"/> to a <see cref="SerializableExpression"/>.
		/// </summary>
		/// <param name="exp">Extended class instance.</param>
		/// <param name="expConverter"><see cref="ExpressionConverter"/> instance.</param>
		/// <returns>Returns the converted <see cref="SerializableExpression"/>.</returns>
		public static SerializableExpression MakeSerializable(this Expression exp, ExpressionConverter expConverter)
		{
			return expConverter.Convert(exp);
		}

		/// <summary>
		/// Extension Method for <see cref="Expression"/>.
		/// Converts an <see cref="Expression"/> to a <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Return type (must be subclass of <see cref="SerializableExpression"/>.</typeparam>
		/// <param name="exp">Extended class instance.</param>
		/// <param name="expConverter"><see cref="ExpressionConverter"/> instance.</param>
		/// <returns>Returns the converted <typeparamref name="T"/>.</returns>
		public static T MakeSerializable<T>(this Expression exp, ExpressionConverter expConverter) where T : SerializableExpression
		{
			return expConverter.Convert<T>(exp);
		}

		/// <summary>
		/// Extension Method for <see cref="IEnumerable"/>.
		/// Converts an <see cref="IEnumerable"/> to a <see cref="ReadOnlyCollection{T}"/>.
		/// </summary>
		/// <typeparam name="T">Return type (must be subclass of <see cref="SerializableExpression"/>.</typeparam>
		/// <param name="exp">Extended class instance.</param>
		/// <param name="expConverter"><see cref="ExpressionConverter"/> instance.</param>
		/// <returns>Returns the converted <see cref="ReadOnlyCollection{T}"/>.</returns>
		public static ReadOnlyCollection<T> MakeSerializableCollection<T>(this IEnumerable exp, ExpressionConverter expConverter) where T : SerializableExpression
		{
			return expConverter.ConvertCollection<T>(exp);
		}

		/// <summary>
		/// Extension Method for <see cref="IEnumerable"/>.
		/// Returns the result of the executed <see cref="SerializableExpression"/>.
		/// </summary>
		/// <param name="exp">Extended class instance.</param>
		/// <param name="linqHandler"><see cref="IQueryHandler"/>.</param>
		/// <returns>Returns the result of the executed <see cref="SerializableExpression"/>.</returns>
		public static object Convert(this SerializableExpression exp, IQueryHandler linqHandler)
		{
			return new SerializableExpressionConverter(exp, linqHandler).Visit();
		}

		/// <summary>
		/// Extension method for <see cref="SerializableExpression"/>.
		/// Returns <see cref="Expression"/>.
		/// </summary>
		/// <param name="exp">Serializable expression to convert</param>
		/// <returns>Returns <see cref="Expression"/>.</returns>
		public static Expression Deserialize(this SerializableExpression exp)
		{
			// http://interlinq.codeplex.com/discussions/60896
			var fakeHandler = new ClientQueryHandler();
			var visitor = new SerializableExpressionConverter(exp, fakeHandler);

			return visitor.Visit(exp);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Zyan.InterLinq.Types.Anonymous
{
	/// <summary>
	/// Serializable abstraction of LINQ's <see cref="IGrouping{TKey, TElement}"/>.
	/// </summary>
	/// <seealso cref="IGrouping{TKey, TElement}"/>
	[Serializable]
	[DataContract]
	public abstract class InterLinqGroupingBase
	{
		/// <summary>
		/// Sets the grouping <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Key to set.</param>
		public abstract void SetKey(object key);

		/// <summary>
		/// Sets the grouping <paramref name="elements"/>.
		/// </summary>
		/// <param name="elements">Elements to set.</param>
		public abstract void SetElements(object elements);
	}

	/// <summary>
	/// Serializable abstraction of LINQ's <see cref="IGrouping{TKey, TElement}"/>.
	/// </summary>
	/// <seealso cref="InterLinqGroupingBase"/>
	/// <seealso cref="IGrouping{TKey, TElement}"/>
	[Serializable]
	[DataContract]
	public class InterLinqGrouping<TKey, TElement> : InterLinqGroupingBase, IGrouping<TKey, TElement>
	{
		#region Properties

		/// <summary>
		/// Gets or sets the elements.
		/// </summary>
		[DataMember]
		public IEnumerable<TElement> Elements { get; set; }

		#endregion

		#region Overridden Methods

		/// <summary>
		/// Sets the grouping <paramref name="elements"/>.
		/// </summary>
		/// <param name="elements">Elements to set.</param>
		public override void SetElements(object elements)
		{
			Elements = (IEnumerable<TElement>)elements;
		}

		/// <summary>
		/// Sets the grouping <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Key to set.</param>
		public override void SetKey(object key)
		{
			Key = (TKey)key;
		}

		#endregion

		#region IGrouping<TKey,TElement> Members

		/// <summary>
		/// Initializes this class.
		/// </summary>
		public InterLinqGrouping()
		{
			Elements = new List<TElement>();
		}

		/// <summary>
		/// Gets or sets the key.
		/// </summary>
		[DataMember]
		public TKey Key { get; set; }

		#endregion

		#region IEnumerable<TElement> Members

		/// <summary>
		/// Returns an <see cref="IEnumerator{T}"/> that iterates through the collection.
		/// </summary>
		/// <returns>
		/// Returns an <see cref="IEnumerator{T}"/> that iterates through the collection.
		/// </returns>
		public IEnumerator<TElement> GetEnumerator()
		{
			return Elements.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		/// <summary>
		/// Returns an <see cref="System.Collections.IEnumerator"/> that iterates through the collection.
		/// </summary>
		/// <returns>
		/// Returns an <see cref="System.Collections.IEnumerator"/> that iterates through the collection.
		/// </returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}

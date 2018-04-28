using System;
using System.Collections;
using System.Collections.Generic;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Generic concurrent collection based on System.ServiceModel.SynchronizedCollection.cs
	/// </summary>
	/// <typeparam name="T">The type of an element.</typeparam>
	public class ConcurrentCollection<T> : IList<T>, IList
	{
		List<T> items;
		object sync;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentCollection{T}"/> class.
		/// </summary>
		public ConcurrentCollection()
		{
			this.items = new List<T>();
			this.sync = new object();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentCollection{T}"/> class.
		/// </summary>
		public ConcurrentCollection(object syncRoot)
		{
			if (syncRoot == null)
				throw new ArgumentNullException("syncRoot");

			this.items = new List<T>();
			this.sync = syncRoot;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentCollection{T}"/> class.
		/// </summary>
		public ConcurrentCollection(object syncRoot, IEnumerable<T> list)
		{
			if (syncRoot == null)
				throw new ArgumentNullException("syncRoot");
			if (list == null)
				throw new ArgumentNullException("list");

			this.items = new List<T>(list);
			this.sync = syncRoot;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentCollection{T}"/> class.
		/// </summary>
		public ConcurrentCollection(object syncRoot, params T[] list)
		{
			if (syncRoot == null)
				throw new ArgumentNullException("syncRoot");
			if (list == null)
				throw new ArgumentNullException("list");

			this.items = new List<T>(list.Length);
			for (int i = 0; i < list.Length; i++)
				this.items.Add(list[i]);

			this.sync = syncRoot;
		}

		/// <summary>
		/// Gets the count.
		/// </summary>
		public int Count
		{
			get { lock (this.sync) { return this.items.Count; } }
		}

		/// <summary>
		/// Gets the items.
		/// </summary>
		protected List<T> Items
		{
			get { return this.items; }
		}

		/// <summary>
		/// Gets the synchronization object.
		/// </summary>
		public object SyncRoot
		{
			get { return this.sync; }
		}

		/// <summary>
		/// Returns an item having the specified index.
		/// </summary>
		/// <param name="index">The index of an item.</param>
		public T this[int index]
		{
			get
			{
				lock (this.sync)
				{
					return this.items[index];
				}
			}
			set
			{
				lock (this.sync)
				{
					if (index < 0 || index >= this.items.Count)
						throw new ArgumentOutOfRangeException("index", index, $"Value must be in range between 0 and {this.items.Count - 1}");

					this.SetItem(index, value);
				}
			}
		}

		/// <summary>
		/// Adds an item to the collection.
		/// </summary>
		/// <param name="item">The item to add.</param>
		public void Add(T item)
		{
			lock (this.sync)
			{
				int index = this.items.Count;
				this.InsertItem(index, item);
			}
		}

		/// <summary>
		/// Clears the collection.
		/// </summary>
		public void Clear()
		{
			lock (this.sync)
			{
				this.ClearItems();
			}
		}

		/// <summary>
		/// Copies the collection to the given array.
		/// </summary>
		/// <param name="array">The array to copy to.</param>
		/// <param name="index">The starting index in the array.</param>
		public void CopyTo(T[] array, int index)
		{
			lock (this.sync)
			{
				this.items.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Checks if the collection contains an item.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Contains(T item)
		{
			lock (this.sync)
			{
				return this.items.Contains(item);
			}
		}

		/// <summary>
		/// Returns the <see cref="IEnumerator{T}"/>.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			lock (this.sync)
			{
				return this.items.GetEnumerator();
			}
		}

		/// <summary>
		/// Gets an index of the given item.
		/// </summary>
		/// <param name="item">The item to search for.</param>
		public int IndexOf(T item)
		{
			lock (this.sync)
			{
				return this.InternalIndexOf(item);
			}
		}

		/// <summary>
		/// Inserts an item at the specified index.
		/// </summary>
		/// <param name="index">The index to insert at.</param>
		/// <param name="item">The item to insert.</param>
		public void Insert(int index, T item)
		{
			lock (this.sync)
			{
				if (index < 0 || index > this.items.Count)
					throw new ArgumentOutOfRangeException("index", index, $"Value must be in range between 0 and {this.items.Count - 1}");

				this.InsertItem(index, item);
			}
		}

		int InternalIndexOf(T item)
		{
			int count = items.Count;

			for (int i = 0; i < count; i++)
			{
				if (object.Equals(items[i], item))
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Removes the specified item.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		public bool Remove(T item)
		{
			lock (this.sync)
			{
				int index = this.InternalIndexOf(item);
				if (index < 0)
					return false;

				this.RemoveItem(index);
				return true;
			}
		}

		/// <summary>
		/// Removes an item at the specified index.
		/// </summary>
		/// <param name="index"></param>
		public void RemoveAt(int index)
		{
			lock (this.sync)
			{
				if (index < 0 || index >= this.items.Count)
					throw new ArgumentOutOfRangeException("index", index, $"Value must be in range between 0 and {this.items.Count - 1}");

				this.RemoveItem(index);
			}
		}

		/// <summary>
		/// Clears all items without locking.
		/// </summary>
		protected virtual void ClearItems()
		{
			this.items.Clear();
		}

		/// <summary>
		/// Inserts an item without locking.
		/// </summary>
		/// <param name="index">The index to insert at.</param>
		/// <param name="item">The item to insert.</param>
		protected virtual void InsertItem(int index, T item)
		{
			this.items.Insert(index, item);
		}

		/// <summary>
		/// Removes an item without locking.
		/// </summary>
		/// <param name="index">The index of an item to remove.</param>
		protected virtual void RemoveItem(int index)
		{
			this.items.RemoveAt(index);
		}

		/// <summary>
		/// Sets the item at the specified index.
		/// </summary>
		/// <param name="index">The index of an item.</param>
		/// <param name="item">The new value of an item.</param>
		protected virtual void SetItem(int index, T item)
		{
			this.items[index] = item;
		}

		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IList)this.items).GetEnumerator();
		}

		bool ICollection.IsSynchronized
		{
			get { return true; }
		}

		object ICollection.SyncRoot
		{
			get { return this.sync; }
		}

		void ICollection.CopyTo(Array array, int index)
		{
			lock (this.sync)
			{
				((IList)this.items).CopyTo(array, index);
			}
		}

		object IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				VerifyValueType(value);
				this[index] = (T)value;
			}
		}

		bool IList.IsReadOnly
		{
			get { return false; }
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		int IList.Add(object value)
		{
			VerifyValueType(value);

			lock (this.sync)
			{
				this.Add((T)value);
				return this.Count - 1;
			}
		}

		bool IList.Contains(object value)
		{
			VerifyValueType(value);
			return this.Contains((T)value);
		}

		int IList.IndexOf(object value)
		{
			VerifyValueType(value);
			return this.IndexOf((T)value);
		}

		void IList.Insert(int index, object value)
		{
			VerifyValueType(value);
			this.Insert(index, (T)value);
		}

		void IList.Remove(object value)
		{
			VerifyValueType(value);
			this.Remove((T)value);
		}

		static void VerifyValueType(object value)
		{
			if (value == null)
			{
				if (typeof(T).IsValueType)
				{
					throw new ArgumentException("ConcurrentCollection cannot operate on value types.");
				}
			}
			else if (!(value is T))
			{
				throw new ArgumentException($"ConcurrentCollection invalid type: {value.GetType().FullName}");
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Tracks subscriptions to compare them between the client and the server.
	/// </summary>
	public class SubscriptionTracker
	{
		/// <summary>
		/// The call context slot name for the subscription checksum.
		/// </summary>
		public const string CallContextSlotName = "subscription-checksum";

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionTracker"/> class.
		/// </summary>
		public SubscriptionTracker()
		{
			checksum = Update();
		}

		private HashSet<Guid> Subscriptions { get; } = new HashSet<Guid>();

		/// <summary>
		/// Gets the subscription count.
		/// </summary>
		public int Count => Subscriptions.Count;

		private string checksum;

		/// <summary>
		/// Gets the checksum of the current subscription set.
		/// </summary>
		public string Checksum
		{
			get
			{
				lock (Subscriptions)
				{
					return checksum;
				}
			}
		}

		/// <summary>
		/// Adds the specified delegate correlation set.
		/// </summary>
		/// <param name="delegateCorrelationSet">The delegate correlation set.</param>
		public string Add(IEnumerable<DelegateCorrelationInfo> delegateCorrelationSet)
		{
			return Add(delegateCorrelationSet.EmptyIfNull().Select(d => d.CorrelationID));
		}

		internal string Add(IEnumerable<Guid> guidsToAdd)
		{
			return Update(add: guidsToAdd);
		}

		/// <summary>
		/// Adds the specified delegate correlation set.
		/// </summary>
		/// <param name="delegateCorrelationSet">The delegate correlation set.</param>
		public string Remove(IEnumerable<DelegateCorrelationInfo> delegateCorrelationSet)
		{
			return Remove(delegateCorrelationSet.EmptyIfNull().Select(d => d.CorrelationID));
		}

		internal string Remove(IEnumerable<Guid> guidsToRemove)
		{
			return Update(remove: guidsToRemove);
		}

		/// <summary>
		/// Resets the subscription tracker, cleans and re-adds all tracked subscriptions.
		/// </summary>
		public string Reset(IEnumerable<DelegateCorrelationInfo> delegateCorrelationSet = null)
		{
			return Reset(delegateCorrelationSet.EmptyIfNull().Select(d => d.CorrelationID));
		}

		internal string Reset(IEnumerable<Guid> add)
		{
			lock (Subscriptions)
			{
				Subscriptions.Clear();
				return Update(add);
			}
		}

		private string Update(IEnumerable<Guid> add = null, IEnumerable<Guid> remove = null)
		{
			lock (Subscriptions)
			{
				Subscriptions.UnionWith(add.EmptyIfNull());
				Subscriptions.ExceptWith(remove.EmptyIfNull());
				return checksum = Subscriptions.Count + ":" + ChecksumHelper.ComputeHash(Subscriptions);
			}
		}
	}
}

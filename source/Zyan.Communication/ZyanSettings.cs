using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication
{
	/// <summary>
	/// Static configuration settings of Zyan.
	/// </summary>
	/// <remarks>
	/// All settings default to false.
	/// </remarks>
	public static class ZyanSettings
	{
		/// <summary>
		/// Gets or sets a value indicating whether URL randomization is disabled.
		/// </summary>
		/// <remarks>
		/// URL randomization is used to work around Remoting Identity caching and is enabled by default.
		/// </remarks>
		public static bool DisableUrlRandomization { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether legacy blocking events raising mode is enabled.
		/// </summary>
		/// <remarks>
		/// Zyan v2.3 and below blocked server threads while raising events.
		/// </remarks>
		public static bool LegacyBlockingEvents { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether legacy blocking subscription mode is enabled.
		/// </summary>
		/// <remarks>
		/// Zyan v2.5 and below blocked client threads while subscribing to events: proxy.Event += handler;
		/// </remarks>
		public static bool LegacyBlockingSubscriptions { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether legacy unprotected events handling mode is enabled.
		/// </summary>
		/// <remarks>
		/// Zyan v2.4 and below didn't prevent client event handlers from throwing exceptions back to server.
		/// </remarks>
		public static bool LegacyUnprotectedEventHandlers { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the ComponentCatalog should ignore duplicate registrations.
		/// </summary>
		/// <remarks>
		/// Zyan v2.7 and below didn't update the existing component registrations.
		/// </remarks>
		public static bool LegacyIgnoreDuplicateRegistrations { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether ZyanConnection restores subscriptions asynchronously.
		/// </summary>
		/// <remarks>
		/// Zyan v2.11 and below used to restore missing event subscriptions asynchronously.
		/// </remarks>
		public static bool LegacyAsyncResubscriptions { get; set; }
	}
}

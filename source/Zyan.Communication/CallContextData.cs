using System;
using System.Collections;

namespace Zyan.Communication
{
	/// <summary>
	/// Stores data that travels with the call context from client to server and back.
	/// </summary>
	[Serializable]
	public class LogicalCallContextData
	{
		// Data store
		private Hashtable _store = null;

		/// <summary>
		/// Creates a new instance of LogicalCallContextData.
		/// </summary>
		public LogicalCallContextData()
		{
			_store = new Hashtable();
		}

		/// <summary>
		/// Gets the data store.
		/// </summary>
		public Hashtable Store
		{
			get { return _store; }
		}
	}

}

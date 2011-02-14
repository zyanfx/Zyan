//=============================================================================
//	The Custom Channel - Knowledge Base 
//	(C) Copyright 2003, Roman Kiss (rkiss@pathcom.com)
//	All rights reserved.
//	The code and information is provided "as-is" without waranty of any kind,
//	either expresed or implied.
//
//-----------------------------------------------------------------------------
//	History:
//		06/05/2003	Roman Kiss				Initial Revision
//=============================================================================
//
#region references
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Runtime.Remoting;
#endregion

namespace RKiss.CustomChannel
{
	public class KnowledgeBase: MarshalByRefObject, IKnowledgeBaseControl
	{
		#region private members
		private ObjRef		m_thisObjRef = null;				// reference to the endpoint
		private bool			m_AllowToUpdate = true;			// allow to update the KB remotely
		private Hashtable m_KB = Hashtable.Synchronized(new Hashtable());
		#endregion

		#region properties
		public Hashtable KB {	get { return m_KB; }}
		public bool AllowToUpdate {	get { return m_AllowToUpdate; } set { m_AllowToUpdate = value; }}
		#endregion

		#region constructor
		public KnowledgeBase()
		{			
		}
		#endregion

		#region control methods
		public void Publish(string endpoint)
		{		
			// publish this object
			m_thisObjRef = RemotingServices.Marshal(this, endpoint);
		}
		#endregion
		
		#region virtual handlers
		public virtual bool OnBeforeChangeKnowledgeBase(string name, string val) { return true; }
		public virtual void OnChangedKnowledgeBase(string name, string val) {}
		public virtual bool OnBeforeClearKnowledgeBase(string name) { return true; }
		public virtual void OnClearKnowledgeBase(string name) {}
		public virtual void OnLoadKnowledgeBase() {}
		#endregion
	
		#region IKnowledgeBaseControl
		public virtual void Save(string filename) 
		{
			throw new NotSupportedException();
		}
		public virtual void Load(string filename) 
		{
			if(AllowToUpdate == false)
				throw new Exception("The Knowledge Base is locked");

			// fire event
			OnLoadKnowledgeBase();

			throw new NotSupportedException();
		}
		public virtual void Store(string name, string val, bool bOverwrite)
		{
			// validation
			if(AllowToUpdate == false)
				throw new Exception("The Knowledge Base is locked");
			if(KB.Contains(name) == true && bOverwrite == false)
				throw new Exception(string.Format("The logical name '{0}' already exist", name));

			// fire event
			if(OnBeforeChangeKnowledgeBase(name, val) == false) 
				return;

			// update/add value in the KB
			KB[name] = val;

			// fire event
			OnChangedKnowledgeBase(name, val);		
		}
		public virtual void Store(string strLURL, bool bOverwrite)
		{
			string[] lurl = strLURL.Split(',');
			foreach(string s in lurl) 
			{
				string[] nv = s.Split(new char[]{'='}, 2);
				if(nv.Length != 2)
					throw new Exception(string.Format("The logical name '{0}' doesn't have a value", s));

				Store(nv[0].Trim(), nv[1].Trim(), bOverwrite);
			}
		}
		public virtual void Update(string name, string val)
		{
			// validation
			if(AllowToUpdate == false)
				throw new Exception("The Knowledge Base is locked");
			if(KB.Contains(name) == false)
				throw new Exception(string.Format("The logical name '{0}' doesn't exist", name));

			// fire event
			if(OnBeforeChangeKnowledgeBase(name, val) == false) 
				return;

			// update value in the KB
			KB[name] = val;

			// fire event
			OnChangedKnowledgeBase(name, val);
		}
		public virtual void Update(string strLURL)
		{
			string[] lurl = strLURL.Split(',');
			foreach(string s in lurl) 
			{
				string[] nv = s.Split(new char[]{'='}, 2);
				if(nv.Length != 2)
					throw new Exception(string.Format("The logical name '{0}' doesn't have a value", s));

				Update(nv[0].Trim(), nv[1].Trim());
			}
		}
		public virtual void RemoveAll()
		{
			if(AllowToUpdate == false)
				throw new Exception("The Knowledge Base is locked");

			// fire event
			if(OnBeforeClearKnowledgeBase(null) == false) 
				return;

			// clear KB
			KB.Clear();

			// fire event
			OnClearKnowledgeBase(null);
		}
		public virtual void Remove(string name)
		{
			// validation
			if(AllowToUpdate == false)
				throw new Exception("The Knowledge Base is locked");
			if(KB.Contains(name) == false)
				throw new Exception(string.Format("The logical name '{0}' doesn't exist", name));

			// fire event
			if(OnBeforeClearKnowledgeBase(name) == false) 
				return;

			// remove name from the KB
			KB.Remove(name);

			// fire event
			OnClearKnowledgeBase(name);
		}
		public virtual object GetAll()
		{
			return KB.Clone();		
		}
		public virtual string Get(string name)
		{
			// validation
			if(KB.Contains(name) == false)
				throw new Exception(string.Format("The logical name '{0}' doesn't exist", name));

			return Convert.ToString(KB[name]);
		}	
		public virtual string Mapping(string name)
		{
			return KB.Contains(name) == true ? Convert.ToString(KB[name]) : name;
		}
		public virtual bool Exists(string name)
		{
			return KB.Contains(name);
		}
		public virtual bool CanBeUpdated()
		{
			return AllowToUpdate;
		}
		#endregion
	
		#region InitializeLifetimeService
		public override object InitializeLifetimeService()
		{
			// infinite lifetime of the remoting access
			return null;
		}
		#endregion
	}
}

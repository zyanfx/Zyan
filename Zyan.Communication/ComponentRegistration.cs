using System;
using System.Collections.Generic;
using Zyan.Communication.Delegates;

namespace Zyan.Communication
{
	/// <summary>
	/// Describes a component registration.
	/// </summary>
	public class ComponentRegistration
	{
		#region Constructors

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		public ComponentRegistration()
		{
			_eventWirings = new Dictionary<Guid, Delegate>();
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="implementationType">Implementation type of the component</param>
		public ComponentRegistration(Type interfaceType, Type implementationType)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = implementationType;
			this.ActivationType = ActivationType.SingleCall;
			this.UniqueName = interfaceType.FullName;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="implementationType">Implementation type of the component</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, Type implementationType, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = implementationType;
			this.ActivationType = ActivationType.SingleCall;
			this.UniqueName = interfaceType.FullName;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="implementationType">Implementation type of the component</param>
		/// <param name="activationType">Activation type (Singleton/SingleCall)</param>
		public ComponentRegistration(Type interfaceType, Type implementationType, ActivationType activationType)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = implementationType;
			this.ActivationType = activationType;
			this.UniqueName = interfaceType.FullName;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="implementationType">Implementation type of the component</param>
		/// <param name="activationType">Activation type (Singleton/SingleCall)</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, Type implementationType, ActivationType activationType, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = implementationType;
			this.ActivationType = activationType;
			this.UniqueName = interfaceType.FullName;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="intializationHandler">Delegate of initialization method</param>
		public ComponentRegistration(Type interfaceType, Func<object> intializationHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.InitializationHandler = intializationHandler;
			this.ActivationType = ActivationType.SingleCall;
			this.UniqueName = interfaceType.FullName;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="intializationHandler">Delegate of initialization method</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.InitializationHandler = intializationHandler;
			this.ActivationType = ActivationType.SingleCall;
			this.UniqueName = interfaceType.FullName;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="intializationHandler">Delegate of initialization method</param>
		/// <param name="activationType">Activation type (Singleton/SingleCall)</param>
		public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, ActivationType activationType)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.InitializationHandler = intializationHandler;
			this.ActivationType = activationType;
			this.UniqueName = interfaceType.FullName;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="intializationHandler">Delegate of initialization method</param>
		/// <param name="activationType">Activation type (Singleton/SingleCall)</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, ActivationType activationType, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.InitializationHandler = intializationHandler;
			this.ActivationType = activationType;
			this.UniqueName = interfaceType.FullName;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="singletonInstance">Singleton instance of the component</param>
		public ComponentRegistration(Type interfaceType, object singletonInstance)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = singletonInstance.GetType();
			this.SingletonInstance = singletonInstance;
			this.ActivationType = ActivationType.Singleton;
			this.UniqueName = interfaceType.FullName;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="singletonInstance">Singleton instance of the component</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, object singletonInstance, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = singletonInstance.GetType();
			this.SingletonInstance = singletonInstance;
			this.ActivationType = ActivationType.Singleton;
			this.UniqueName = interfaceType.FullName;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="implementationType">Implementation type of the component</param>
		/// <param name="uniqueName">Unique component name</param>
		public ComponentRegistration(Type interfaceType, Type implementationType, string uniqueName)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = implementationType;
			this.UniqueName = uniqueName;
			this.ActivationType = ActivationType.SingleCall;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="implementationType">Implementation type of the component</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, Type implementationType, string uniqueName, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = implementationType;
			this.UniqueName = uniqueName;
			this.ActivationType = ActivationType.SingleCall;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="implementationType">Implementation type of the component</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type (Singleton/SingleCall)</param>
		public ComponentRegistration(Type interfaceType, Type implementationType, string uniqueName, ActivationType activationType)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = implementationType;
			this.UniqueName = uniqueName;
			this.ActivationType = activationType;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="implementationType">Implementation type of the component</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type (Singleton/SingleCall)</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, Type implementationType, string uniqueName, ActivationType activationType, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = implementationType;
			this.UniqueName = uniqueName;
			this.ActivationType = activationType;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="intializationHandler">Delegate of initialization method</param>
		/// <param name="uniqueName">Unique component name</param>
		public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, string uniqueName)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.InitializationHandler = intializationHandler;
			this.UniqueName = uniqueName;
			this.ActivationType = ActivationType.SingleCall;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="intializationHandler">Delegate of initialization method</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, string uniqueName, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.InitializationHandler = intializationHandler;
			this.UniqueName = uniqueName;
			this.ActivationType = ActivationType.SingleCall;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="intializationHandler">Delegate of initialization method</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type (Singleton/SingleCall)</param>
		public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, string uniqueName, ActivationType activationType)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.InitializationHandler = intializationHandler;
			this.UniqueName = uniqueName;
			this.ActivationType = activationType;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="intializationHandler">Delegate of initialization method</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type (Singleton/SingleCall)</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, string uniqueName, ActivationType activationType, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.InitializationHandler = intializationHandler;
			this.UniqueName = uniqueName;
			this.ActivationType = activationType;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="singletonInstance">Singleton instance of the component</param>
		/// <param name="uniqueName">Unique component name</param>
		public ComponentRegistration(Type interfaceType, object singletonInstance, string uniqueName)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = singletonInstance.GetType();
			this.SingletonInstance = singletonInstance;
			this.UniqueName = uniqueName;
			this.ActivationType = ActivationType.Singleton;
			this.EventStub = new EventStub(InterfaceType);
		}

		/// <summary>
		/// Creates a new instance of the ComponentRegistration class.
		/// </summary>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <param name="singletonInstance">Singleton instance of the component</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="cleanUpHandler">Delegate of clean up method</param>
		public ComponentRegistration(Type interfaceType, object singletonInstance, string uniqueName, Action<object> cleanUpHandler)
			: this()
		{
			this.InterfaceType = interfaceType;
			this.ImplementationType = singletonInstance.GetType();
			this.SingletonInstance = singletonInstance;
			this.UniqueName = uniqueName;
			this.ActivationType = ActivationType.Singleton;
			this.CleanUpHandler = cleanUpHandler;
			this.EventStub = new EventStub(InterfaceType);
		}

		#endregion

		#region Properties

		private object _syncLock = new object();

		/// <summary>
		/// Returns the lock object for thread synchronization.
		/// </summary>
		public object SyncLock
		{
			get { return _syncLock; }
		}

		private Dictionary<Guid, Delegate> _eventWirings;

		/// <summary>
		/// Returns a name-value-list of registered event wirings.
		/// </summary>
		internal Dictionary<Guid, Delegate> EventWirings
		{
			get { return _eventWirings; }
		}

		/// <summary>
		/// Gets or sets the unqiue name of the component.
		/// </summary>
		public string UniqueName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the interface type of the component. 
		/// </summary>
		public Type InterfaceType
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the implementation type of the component.
		/// </summary>
		public Type ImplementationType
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the delegate of the initialization method.
		/// </summary>
		public Func<object> InitializationHandler
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the current instance (Singleton activation only) of the registered component.
		/// </summary>
		public object SingletonInstance
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the event stub that caches all event handlers of the registered component.
		/// </summary>
		public EventStub EventStub
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the activation type (Singleton/SingleCall)
		/// </summary>
		public ActivationType ActivationType
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets, if the components should be disposed together with its owning component catalog.
		/// </summary>
		public bool DisposeWithCatalog
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a delegate of a method for handling resource clean up explicitly.
		/// </summary>
		public Action<object> CleanUpHandler
		{
			get;
			set;
		}

		#endregion

		/// <summary>
		/// Returns a string representation of the object.
		/// </summary>
		/// <returns>Unique name of the component</returns>
		public override string ToString()
		{
			return this.UniqueName;
		}
	}
}

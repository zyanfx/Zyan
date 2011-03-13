using System;
using System.Collections.Generic;

namespace Zyan.Communication
{
    /// <summary>
    /// Aufzählung der verfügbaren Aktivierungsarten.
    /// </summary>
    public enum ActivationType : short
    {
        /// <summary>
        /// Komponenteninstanz lebt nur einen Aufruf lang. Für jeden Aufruf wird eine separate Instanz erzeugt.
        /// <remarks>SingleCallaktivierte Komponenten müssen nicht threadsicher sein.</remarks>
        /// </summary>
        SingleCall = 1,
        /// <summary>
        /// Komponenteninstanz wird bei erstem Aufruf erzeugt und wird für alle weiteren Aufrufe wiederverwendet.
        /// <remarks>Singltonaktivierte Komponenten müssen threadsicher sein.</remarks>
        /// </summary>
        Singleton
    }

    /// <summary>
    /// Beschreibt eine Komponenten-Registrierung.
    /// </summary>
    public class ComponentRegistration
    {
        // Sperrobjekt für Threadsynchronisierung
        private object _syncLock = new object();

        /// <summary>
        /// Gibt das Sperrobjekt für Threadsynchronisierung zurück.
        /// </summary>
        public object SyncLock
        {
            get { return _syncLock; }
        }

        // Liste mit Ereignisverdrahtungen
        private Dictionary<Guid,Delegate> _eventWirings;

        /// <summary>
        /// Gibt die Liste der Ereignisverdrahtungen zurück.
        /// </summary>
        internal Dictionary<Guid, Delegate> EventWirings
        {
            get { return _eventWirings; }
        }

        /// <summary>
        /// Standardkonstruktor.
        /// </summary>
        public ComponentRegistration()
        {
            // Liste für Ereignisverdrahtungen erzeugen
            _eventWirings = new Dictionary<Guid, Delegate>();
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="implementationType">Implementierungstyp der Komponente</param>                
        public ComponentRegistration(Type interfaceType, Type implementationType) 
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = implementationType;
            this.ActivationType = ActivationType.SingleCall;
            this.UniqueName = interfaceType.FullName;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="implementationType">Implementierungstyp der Komponente</param>                
        /// <param name="activationType">Aktivierungsart</param>
        public ComponentRegistration(Type interfaceType, Type implementationType, ActivationType activationType)
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = implementationType;
            this.ActivationType = activationType;
            this.UniqueName = interfaceType.FullName;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="intializationHandler">Delegat auf Inizialisierungsfunktion</param>        
        public ComponentRegistration(Type interfaceType, Func<object> intializationHandler)
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;            
            this.InitializationHandler = intializationHandler;
            this.ActivationType = ActivationType.SingleCall;
            this.UniqueName = interfaceType.FullName;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="intializationHandler">Delegat auf Inizialisierungsfunktion</param>        
        /// <param name="activationType">Aktivierungsart</param>
        public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, ActivationType activationType)
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.InitializationHandler = intializationHandler;
            this.ActivationType = activationType;
            this.UniqueName = interfaceType.FullName;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="singletonInstance">Singleton-Instanz der Komponente</param>
        public ComponentRegistration(Type interfaceType, object singletonInstance)
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = singletonInstance.GetType();
            this.SingletonInstance = singletonInstance;
            this.ActivationType = ActivationType.Singleton;
            this.UniqueName = interfaceType.FullName;
        }
                
        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="implementationType">Implementierungstyp der Komponente</param>        
        /// <param name="uniqueName">Eindeutiger Name</param>
        public ComponentRegistration(Type interfaceType, Type implementationType, string uniqueName)
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = implementationType;
            this.UniqueName = uniqueName;
            this.ActivationType = ActivationType.SingleCall;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="implementationType">Implementierungstyp der Komponente</param>        
        /// <param name="uniqueName">Eindeutiger Name</param>
        /// <param name="activationType">Aktivierungsart</param>
        public ComponentRegistration(Type interfaceType, Type implementationType, string uniqueName, ActivationType activationType)
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = implementationType;
            this.UniqueName = uniqueName;
            this.ActivationType = activationType;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="intializationHandler">Delegat auf Inizialisierungsfunktion</param>        
        /// <param name="uniqueName">Eindeutiger Name</param>
        public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, string uniqueName)
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.InitializationHandler = intializationHandler;
            this.UniqueName = uniqueName;
            this.ActivationType = ActivationType.SingleCall;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="intializationHandler">Delegat auf Inizialisierungsfunktion</param>        
        /// <param name="uniqueName">Eindeutiger Name</param>
        /// <param name="activationType">Aktivierungsart</param>
        public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, string uniqueName, ActivationType activationType)
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.InitializationHandler = intializationHandler;
            this.UniqueName = uniqueName;
            this.ActivationType = activationType;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="singletonInstance">Singleton-Instanz der Komponente</param>
        /// <param name="uniqueName">Eindeutiger Name</param>
        public ComponentRegistration(Type interfaceType, object singletonInstance, string uniqueName)
            : this()
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = singletonInstance.GetType();
            this.SingletonInstance = singletonInstance;
            this.UniqueName = uniqueName;
            this.ActivationType = ActivationType.Singleton;
        }

        /// <summary>
        /// Gibt den eindeutigen Namen zurück, oder legt ihn fest.        
        /// </summary>
        public string UniqueName
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt den Typ der öffentlichen Komponenten-Schnittstelle zurück, oder legt ihn fest. 
        /// </summary>
        public Type InterfaceType
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt den Implementierungstyp der Komponente zurück, oder legt ihn fest.
        /// </summary>
        public Type ImplementationType
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt einen Delegaten auf eine Methode zurück, die sich um die Erzeugung der Komponente und deren Inizialisierung kümmert,
        /// </summary>
        public Func<object> InitializationHandler
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt die Singleton-Instanz der Komponente zurück, oder legt diese fest.
        /// <remarks>
        /// Wenn keine Singleton-Instanz angegeben ist, erzeugt der Komponentenhost für jeden Client-Aufruf 
        /// automatisch eine Instanz, die nur für einen Methodenaufruf lebt.
        /// </remarks>
        /// </summary>
        public object SingletonInstance
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt die Aktivierungsart der Komponente zurück, oder lget sie fest.
        /// </summary>
        public ActivationType ActivationType
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt die Objektdaten als Zeichenkette zurück.
        /// </summary>
        /// <returns>Als Zeichenkette ausgedrückte Objektdaten</returns>
        public override string ToString()
        {
            // Eindeutigen Namen zurückgeben
            return this.UniqueName;
        }
    }
}

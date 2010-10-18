using System;

namespace Zyan.Communication
{
    /// <summary>
    /// Beschreibt eine Komponenten-Registrierung.
    /// </summary>
    public class ComponentRegistration
    {
        /// <summary>
        /// Standardkonstruktor.
        /// </summary>
        public ComponentRegistration()
        {

        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="implementationType">Implementierungstyp der Komponente</param>        
        /// <param name="syncBehavior">Sync/Async Verhalten der Komponente</param>
        public ComponentRegistration(Type interfaceType, Type implementationType) 
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = implementationType;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="intializationHandler">Delegat auf Inizialisierungsfunktion</param>
        /// <param name="syncBehavior">Sync/Async Verhalten der Komponente</param>
        public ComponentRegistration(Type interfaceType, Func<object> intializationHandler)
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;            
            this.InitializationHandler = intializationHandler;            
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="singletonInstance">Singleton-Instanz der Komponente</param>
        public ComponentRegistration(Type interfaceType, object singletonInstance)
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = singletonInstance.GetType();
            this.SingletonInstance = singletonInstance;            
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="implementationType">Implementierungstyp der Komponente</param>        
        /// <param name="moduleName">Modulname</param>
        public ComponentRegistration(Type interfaceType, Type implementationType, string moduleName)
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = implementationType;
            this.ModuleName = moduleName;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="intializationHandler">Delegat auf Inizialisierungsfunktion</param>
        /// <param name="syncBehavior">Sync/Async Verhalten der Komponente</param>
        /// <param name="moduleName">Modulname</param>
        public ComponentRegistration(Type interfaceType, Func<object> intializationHandler, string moduleName)
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.InitializationHandler = intializationHandler;
            this.ModuleName = moduleName;
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente</param>
        /// <param name="singletonInstance">Singleton-Instanz der Komponente</param>
        /// <param name="moduleName">Modulname</param>
        public ComponentRegistration(Type interfaceType, object singletonInstance, string moduleName)
        {
            // Werte übernehmen
            this.InterfaceType = interfaceType;
            this.ImplementationType = singletonInstance.GetType();
            this.SingletonInstance = singletonInstance;
            this.ModuleName = moduleName;
        }

        /// <summary>
        /// Gibt den Modulnamen zurück, oder legt ihn fest.        
        /// </summary>
        public string ModuleName
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
        /// Gibt die Objektdaten als Zeichenkette zurück.
        /// </summary>
        /// <returns>Als Zeichenkette ausgedrückte Objektdaten</returns>
        public override string ToString()
        {
            // Vollständiger Name der Schnittstelle zurückgeben
            return this.InterfaceType.FullName;
        }
    }
}

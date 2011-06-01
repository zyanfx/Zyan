using System;
using System.Collections.Generic;

namespace Zyan.Communication
{
    /// <summary>
    /// Komponenten-Katalog
    /// </summary>
    public class ComponentCatalog
    {
        #region Komponenten-Hosting

        // Liste der Registrierten Komponenten
        private Dictionary<string, ComponentRegistration> _componentRegistry = null;

        /// <summary>
        /// Ruft eine bestimmte Komponentenregistrierung ab.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp</param>
        /// <returns>Komponentenregistrierung</returns>
        public ComponentRegistration GetRegistration(Type interfaceType)
        {
            // Wenn kein Schnittstellentyp angegeben wurde ...
            if (interfaceType.Equals(null))
                // Ausnahme werfen
                throw new ArgumentNullException("interfaceType");

            // Registrierung abrufen
            return GetRegistration(interfaceType.FullName);
        }

        /// <summary>
        /// Ruft eine bestimmte Komponentenregistrierung ab.
        /// </summary>
        /// <param name="interfaceName">Schnittstellenname</param>
        /// <returns>Komponentenregistrierung</returns>
        public ComponentRegistration GetRegistration(string interfaceName)
        {
            // Wenn für den angegebenen Schnittstellennamen keine Komponente registriert ist ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
                // Ausnahme werfen
                throw new KeyNotFoundException(string.Format("Für die angegebene Schnittstelle '{0}' ist keine Komponente registiert.", interfaceName));

            // Komponentenregistrierung abrufen
            return ComponentRegistry[interfaceName];
        }

        /// <summary>
        /// Gibt die Liste der Registrierten Komponenten zurück.
        /// <remarks>Falls die Liste noch nicht existiert, wird sie automatisch erstellt.</remarks>
        /// </summary>
        internal Dictionary<string, ComponentRegistration> ComponentRegistry
        {
            get
            {
                // Wenn die Liste der Registrierten Komponenten noch nicht erzeugt wurde ...
                if (_componentRegistry == null)
                    // Liste erzeugen
                    _componentRegistry = new Dictionary<string, ComponentRegistration>();

                // Liste zurückgeben
                return _componentRegistry;
            }
        }

        /// <summary>
        /// Hebt die Registrierung einer bestimmten Komponente auf.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        public void UnregisterComponent<I>()
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);

            // Wenn eine Komponente mit der angegebenen Schnittstelle registriert ist ...
            if (ComponentRegistry.ContainsKey(interfaceType.FullName))
            {
                // Registrierung aufheben
                ComponentRegistry.Remove(interfaceType.FullName);
            }
        }

        /// <summary>
        /// Hebt die Registrierung einer bestimmten Komponente auf.
        /// </summary>
        /// <param name="uniqueName">Eindeutiger Name</param>
        public void UnregisterComponent(string uniqueName)
        {
            // Wenn eine Komponente mit der angegebenen Schnittstelle registriert ist ...
            if (ComponentRegistry.ContainsKey(uniqueName))
            {
                // Registrierung aufheben
                ComponentRegistry.Remove(uniqueName);
            }
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>        
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        public void RegisterComponent<I, T>()
        {
            // Andere Überladung aufrufen
            RegisterComponent<I, T>(ActivationType.SingleCall);
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>        
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        /// <param name="uniqueName">Eindeutiger Name der Komponente</param>
        public void RegisterComponent<I, T>(string uniqueName)
        {
            // Andere Überladung aufrufen
            RegisterComponent<I, T>(uniqueName, ActivationType.SingleCall);
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>        
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        /// <param name="activationType">Aktivierungsart</param>
        public void RegisterComponent<I, T>(ActivationType activationType)
        {
            RegisterComponent<I, T>(string.Empty, activationType);
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>        
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        /// <param name="uniqueName">Eindeutiger Name der Komponente</param>
        /// <param name="activationType">Aktivierungsart</param>
        public void RegisterComponent<I, T>(string uniqueName, ActivationType activationType)
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);
            Type implementationType = typeof(T);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_TypeIsNotAInterface, "interfaceType");

            // Wenn der Implementierungstyp keine Klasse ist ...
            if (!implementationType.IsClass)
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_TypeIsNotAClass, "interfaceType");

            // Wenn kein eindeutiger Name angegeben ist ...
            if (string.IsNullOrEmpty(uniqueName))
                // Name der Schnittstelle verwenden
                uniqueName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(uniqueName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, implementationType, uniqueName, activationType);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(uniqueName, registration);
            }
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <param name="factoryMethod">Delegat auf Fabrikmethode, die sich um die Erzeugung und Inizialisierung der Komponente kümmert</param>
        public void RegisterComponent<I>(Func<object> factoryMethod)
        {
            // Andere Überladung aufrufen
            RegisterComponent<I>(factoryMethod, ActivationType.SingleCall);
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <param name="uniqueName">Eindeutiger Name der Komponente</param>
        /// <param name="factoryMethod">Delegat auf Fabrikmethode, die sich um die Erzeugung und Inizialisierung der Komponente kümmert</param>
        public void RegisterComponent<I>(string uniqueName, Func<object> factoryMethod)
        {
            // Andere Überladung aufrufen
            RegisterComponent<I>(uniqueName, factoryMethod, ActivationType.SingleCall);
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <param name="factoryMethod">Delegat auf Fabrikmethode, die sich um die Erzeugung und Inizialisierung der Komponente kümmert</param>
        /// <param name="activationType">Aktivierungsart</param>
        public void RegisterComponent<I>(Func<object> factoryMethod, ActivationType activationType)
        {
            RegisterComponent<I>(string.Empty, factoryMethod, activationType);
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <param name="uniqueName">Eindeutiger Name, über den die Komponente gefunden werden kann</param>
        /// <param name="factoryMethod">Delegat auf Fabrikmethode, die sich um die Erzeugung und Inizialisierung der Komponente kümmert</param>
        /// <param name="activationType">Aktivierungsart</param>
        public void RegisterComponent<I>(string uniqueName, Func<object> factoryMethod, ActivationType activationType)
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ApplicationException(LanguageResource.ArgumentException_TypeIsNotAInterface);

            // Wenn kein Delegat auf eine Fabrikmethode angegeben wurde ...
            if (factoryMethod == null)
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_FactoryMethodDelegateMissing, "factoryMethod");

            // Wenn kein eindeutiger Name angegeben ist ...
            if (string.IsNullOrEmpty(uniqueName))
                // Name der Schnittstelle verwenden
                uniqueName = interfaceType.FullName;
            
            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(uniqueName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, factoryMethod, uniqueName, activationType);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(uniqueName, registration);
            }
        }

        /// <summary>
        /// Registriert eine bestimmte Komponenteninstanz.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>        
        /// <param name="instance">Instanz</param>
        public void RegisterComponent<I, T>(T instance)
        {
            RegisterComponent<I, T>(string.Empty, instance);
        }

        /// <summary>
        /// Registriert eine bestimmte Komponenteninstanz.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>        
        /// <param name="uniqueName">Eindeutiger Name der Komponente</param>
        /// <param name="instance">Instanz</param>
        public void RegisterComponent<I, T>(string uniqueName,T instance)
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);
            Type implementationType = typeof(T);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_TypeIsNotAInterface, "interfaceType");

            // Wenn der Implementierungstyp keine Klasse ist ...
            if (!implementationType.IsClass)
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_TypeIsNotAClass, "interfaceType");

            // Wenn kein eindeutiger Name angegeben ist ...
            if (string.IsNullOrEmpty(uniqueName))
                // Name der Schnittstelle verwenden
                uniqueName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(uniqueName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, instance, uniqueName);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(uniqueName, registration);
            }
        }
        
                
        /// <summary>
        /// Gibt eine Liste mit allen registrierten Komponenten zurück.
        /// </summary>
        /// <returns>Liste der registrierten Komponenten</returns>
        public List<ComponentInfo> GetRegisteredComponents()
        {
            // Wörterbuch erzeugen 
            List<ComponentInfo> result = new List<ComponentInfo>();

            // Komponentenregistrierung druchlaufen
            foreach (ComponentRegistration registration in ComponentRegistry.Values)
            {
                // Neuen Eintrag erstellen
                result.Add(new ComponentInfo()
                           {
                               InterfaceName = registration.InterfaceType.FullName,
                               UniqueName = registration.UniqueName,
                               ActivationType = registration.ActivationType
                           });
            }
            // Wörterbuch zurückgeben
            return result;
        }
        
        /// <summary>
        /// Gibt eine Instanz einer registrierten Komponente zurück.
        /// </summary>
        /// <param name="registration">Komponentenregistrierung</param>
        /// <returns>Komponenten-Instanz</returns>
        internal object GetComponentInstance(ComponentRegistration registration)
        {
            // Wenn keine Komponentenregistrierung angegeben wurde ...
            if (registration == null)
                // Ausnahme werfen
                throw new ArgumentNullException("registration");

            // Aktivierungsart auswerten
            switch (registration.ActivationType)
            { 
                case ActivationType.SingleCall: // SingleCall-Aktivierung
                    
                    // Wenn eine Initialisierungsfunktion angegeben wurde ...
                    if (registration.InitializationHandler != null)
                        // Komponenteninstanz von Inizialisierungsfunktion erzeugen lassen
                        return registration.InitializationHandler();
                    else
                        // Komponenteninstanz erzeugen
                        return Activator.CreateInstance(registration.ImplementationType);

                case ActivationType.Singleton: // Singleton-Aktivierung

                    // Wenn noch keine Singleton-Instanz existiert ...
                    if (registration.SingletonInstance == null)
                    {
                        lock (registration.SyncLock)
                        { 
                            // Wenn die Singleton-Instanz nicht bereits durch einen anderen Thread erzeugt wurde ...
                            if (registration.SingletonInstance == null)
                            {
                                // Wenn eine Initialisierungsfunktion angegeben wurde ...
                                if (registration.InitializationHandler!=null)
                                    // Singleton-Instanz mittels Inizialisierungsfunktion erzeugen
                                    registration.SingletonInstance = registration.InitializationHandler();
                                else
                                    // Singleton-Instanz erzeugen
                                    registration.SingletonInstance = Activator.CreateInstance(registration.ImplementationType);
                            }
                        }
                    }
                    // Singleton-Instanz zurückgeben
                    return registration.SingletonInstance;
            }
            // Ausnahme werfen
            throw new NullReferenceException();
        }

        /// <summary>
        /// Gibt eine Instanz einer bestimmten Komponente zurück.
        /// </summary>
        /// <typeparam name="I">Typ der Komponentenschnittstelle</typeparam>
        /// <returns>Komponenteninstanz</returns>
        public I GetComponent<I>()
        {
            // Schnittstellenname abrufen
            string interfaceName=typeof(I).FullName;

            // Komponenteninstanz erzeugen und zurückgeben
            return (I)GetComponentInstance(ComponentRegistry[interfaceName]);
        }

        /// <summary>
        /// Gibt eine Instanz einer bestimmten Komponente zurück.
        /// </summary>
        /// <param name="uniqueName">Eindeutiger Name der Komponente</param>
        /// <returns>Komponenteninstanz</returns>
        public object GetComponent(string uniqueName)
        {            
            // Komponenteninstanz erzeugen und zurückgeben
            return GetComponentInstance(ComponentRegistry[uniqueName]);
        }
        
        #endregion               
    }
}

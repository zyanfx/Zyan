using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            if (interfaceType == null)
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
        /// Registriert eine bestimmte Komponente.
        /// </summary>        
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        public void RegisterComponent<I, T>()
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);
            Type implementationType = typeof(T);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Schnittstellentyp ist keine Schnittstelle!", "interfaceType");

            // Wenn der Implementierungstyp keine Klasse ist ...
            if (!implementationType.IsClass)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Implementierungstyp ist keine Klasse!", "interfaceType");

            // Name der Schnittstelle abfragen
            string interfaceName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, implementationType);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(interfaceName, registration);
            }
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <param name="factoryMethod">Delegat auf Fabrikmethode, die sich um die Erzeugung und Inizialisierung der Komponente kümmert</param>
        public void RegisterComponent<I>(Func<object> factoryMethod)
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ApplicationException("Der angegebene Schnittstellentyp ist keine Schnittstelle!");

            // Wenn kein Delegat auf eine Fabrikmethode angegeben wurde ...
            if (factoryMethod == null)
                // Ausnahme werfen
                throw new ArgumentException("Keinen Delegaten für Komponentenerzeugung angegeben.", "factoryMethod");

            // Name der Schnittstelle abfragen
            string interfaceName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, factoryMethod);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(interfaceName, registration);
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
            // Typinformationen abrufen
            Type interfaceType = typeof(I);
            Type implementationType = typeof(T);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Schnittstellentyp ist keine Schnittstelle!", "interfaceType");

            // Wenn der Implementierungstyp keine Klasse ist ...
            if (!implementationType.IsClass)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Implementierungstyp ist keine Klasse!", "interfaceType");

            // Name der Schnittstelle abfragen
            string interfaceName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, instance);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(interfaceName, registration);
            }
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>        
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        /// <param name="moduleName">Modulname</param>
        public void RegisterComponent<I, T>(string moduleName)
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);
            Type implementationType = typeof(T);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Schnittstellentyp ist keine Schnittstelle!", "interfaceType");

            // Wenn der Implementierungstyp keine Klasse ist ...
            if (!implementationType.IsClass)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Implementierungstyp ist keine Klasse!", "interfaceType");

            // Name der Schnittstelle abfragen
            string interfaceName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, implementationType, moduleName);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(interfaceName, registration);
            }
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <param name="moduleName">Modulname</param>
        /// <param name="factoryMethod">Delegat auf Fabrikmethode, die sich um die Erzeugung und Inizialisierung der Komponente kümmert</param>
        public void RegisterComponent<I>(string moduleName, Func<object> factoryMethod)
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ApplicationException("Der angegebene Schnittstellentyp ist keine Schnittstelle!");

            // Wenn kein Delegat auf eine Fabrikmethode angegeben wurde ...
            if (factoryMethod == null)
                // Ausnahme werfen
                throw new ArgumentException("Keinen Delegaten für Komponentenerzeugung angegeben.", "factoryMethod");

            // Name der Schnittstelle abfragen
            string interfaceName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, factoryMethod, moduleName);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(interfaceName, registration);
            }
        }

        /// <summary>
        /// Registriert eine bestimmte Komponenteninstanz.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        /// <param name="moduleName">Modulname</param>
        /// <param name="instance">Instanz</param>
        public void RegisterComponent<I, T>(string moduleName, T instance)
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);
            Type implementationType = typeof(T);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Schnittstellentyp ist keine Schnittstelle!", "interfaceType");

            // Wenn der Implementierungstyp keine Klasse ist ...
            if (!implementationType.IsClass)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Implementierungstyp ist keine Klasse!", "interfaceType");

            // Name der Schnittstelle abfragen
            string interfaceName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, instance, moduleName);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(interfaceName, registration);
            }
        }
        
        /// <summary>
        /// Gibt eine Liste mit allen registrierten Komponenten zurück.
        /// </summary>
        /// <returns>Liste der registrierten Komponenten</returns>
        public List<string> GetRegisteredComponents()
        {
            // Wörterbuch erzeugen 
            List<string> result = new List<string>();

            // Komponentenregistrierung druchlaufen
            foreach (ComponentRegistration registration in ComponentRegistry.Values)
            {
                // Neuen Eintrag erstellen
                result.Add(registration.InterfaceType.FullName);
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

            // Wenn eine Singleton-Instanz registriert wurde ...
            if (registration.SingletonInstance != null)
                // Singleton-Instanz zurückgeben
                return registration.SingletonInstance;

            // Wenn eine Initialisierungsfunktion angegeben wurde ...
            if (registration.InitializationHandler != null)
                // Komponenteninstanz von Inizialisierungsfunktion erzeugen lassen
                return registration.InitializationHandler();
            else
                // Komponenteninstanz erzeugen
                return Activator.CreateInstance(registration.ImplementationType);
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
        
        #endregion               
    }
}

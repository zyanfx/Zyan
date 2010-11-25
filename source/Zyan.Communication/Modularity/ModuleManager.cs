using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.SessionMgmt;

namespace Zyan.Communication.Modularity
{
    /// <summary>
    /// Verwaltet und Startet Module.
    /// </summary>
    public static class ModuleManager
    {
        /// <summary>
        /// Gibt den Anwendungsnamen zurück.
        /// </summary>
        public static string ApplicationName { get; private set; }

        /// <summary>
        /// Gibt den lokalen Komponenten-Katalog zurück.
        /// </summary>
        public static ComponentCatalog LocalComponents { get; private set; }
        
        /// <summary>
        /// Gibt eine Liste der geladenen Module zurück.
        /// </summary>
        public static List<ModuleHost> Modules { get; private set; }

        /// <summary>
        /// Gibt das Bereitstellungsverzeichnis der Anwendung zurück.
        /// </summary>
        public static string DeploymentDirectory { get; private set; }
        
        /// <summary>
        /// Inizialisiert die Modulverwaltung.
        /// </summary>
        /// <param name="applicationName">Anwendungsname</param>
        /// <param name="deploymentDir">Bereitstellungsverzeichnis der Anwendung</param>        
        public static void Init(string applicationName, string deploymentDir)
        {
            // Wenn kein Anwendungsname angegeben wurde ...
            if (string.IsNullOrEmpty(applicationName))
                // Ausnahme werfen
                throw new ArgumentException();

            // Wenn kein Bereitstellungsverzeichnis angegeben wurde ...
            if (string.IsNullOrEmpty(deploymentDir))
                // Ausnahme werfen
                throw new ArgumentException();

            // Wenn das angegebene Bereitstellungsverzeichnis nicht existiert ...
            if (!Directory.Exists(deploymentDir))
                // Ausnahme werfen
                throw new DirectoryNotFoundException(deploymentDir);
            
            // Anwendungsname übernehmen
            ApplicationName = applicationName;

            // Bereitstellungsverzeicnis übernehmen
            DeploymentDirectory = deploymentDir;

            // Modul-Auflistung erzeugen
            Modules = new List<ModuleHost>();
            
            // Lokalen Komponenten-Katalog erzeugen
            LocalComponents = new ComponentCatalog();            
        }

        // Sperrobjekt für das Laden von Modulen
        private static object _loadModulesLockObject = new object();

        /// <summary>
        /// Lädt die Module der Anwendung.
        /// </summary>        
        private static void LoadModules()
        {
            lock (_loadModulesLockObject)
            {
                // Wenn noch keine Module geladen sind ...
                if (Modules.Count == 0)
                {
                    // Modul-Verzeichnisse abrufen
                    string[] moduleDirs = Directory.GetDirectories(DeploymentDirectory, "*.*", SearchOption.TopDirectoryOnly);

                    // Alle Modul-Verzeichnisse durchlaufen
                    foreach (string moduleDir in moduleDirs)
                    {
                        // Modul Host erzeugen
                        ModuleHost host = new ModuleHost(moduleDir);
                        
                        // Modul Host der Modul Host-Auflistung zufügen
                        Modules.Add(host);
                    }
                }
            }
        }

        /// <summary>
        /// Gibt zurück, ob die Module laufen.
        /// </summary>
        public static bool IsRunning { get; private set; }

        // Sperrobjekt für die IsRunning-Abfrage
        private static object _isRunningLockObject = new object();

        /// <summary>
        /// Startet die Module.
        /// </summary>
        public static void Start()
        {
            lock (_isRunningLockObject)
            {
                // Wenn die Module noch nicht laufen ...
                if (!IsRunning)
                {
                    // Ggf. Module laden
                    LoadModules();

                    // Alle Module durchlaufen
                    foreach (ModuleHost host in Modules)
                    {
                        // Modul starten
                        host.StartModule();
                    }
                    // Anwendung als laufend markieren
                    IsRunning = true;
                }
            }
        }

        /// <summary>
        /// Stoppt die Module.
        /// </summary>
        public static void Stop()
        {
            lock (_isRunningLockObject)
            {
                // Wenn die Anwendung läuft ...
                if (IsRunning)
                {
                    // Alle Module durchlaufen
                    foreach (ModuleHost host in Modules)
                    {
                        // Modul stoppen
                        host.StopModule();
                    }
                    // Anwendung als gestoppt markieren
                    IsRunning = false;
                }
            }
        }
    }
}

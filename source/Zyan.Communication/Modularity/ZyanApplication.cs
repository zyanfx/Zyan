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
    /// Kern einer Zyan-Aanwendung.
    /// </summary>
    public class ZyanApplication
    {
        // Auflistung der Anwendungen
        private static Dictionary<string, ZyanApplication> _applications = new Dictionary<string, ZyanApplication>();

        /// <summary>
        /// Gibt eine bestimmte Anwendung zurück.
        /// </summary>
        /// <param name="applicationName">Anwendungsname</param>
        /// <returns>Anwendungs-Stammobjekt</returns>
        public static ZyanApplication GetApplication(string applicationName)
        { 
            // Wenn kein Anwendungsname übergeben wurde ...
            if (string.IsNullOrEmpty(applicationName))
                // Ausnahme werfen
                throw new ArgumentException();

            // Wenn der angegebene Anwendungsname nicht gefunden wurde ...
            if (!_applications.ContainsKey(applicationName))
                // Ausnahme werfen
                throw new KeyNotFoundException();

            // Anwendung zurückgeben
            return _applications[applicationName];
        }

        /// <summary>
        /// Gibt eine Liste der Applikationen zurück.
        /// </summary>
        public static List<ZyanApplication> Applications
        {
            get { return _applications.Values.ToList<ZyanApplication>(); }
        }

        /// <summary>
        /// Gibt den Anwendungsnamen zurück.
        /// </summary>
        public string ApplicationName { get; private set; }

        /// <summary>
        /// Gibt die Serververbindung zurück, oder legt sie fest.
        /// </summary>
        public ZyanConnection ServerConnection { get; set; }

        /// <summary>
        /// Gibt den Komponenten Host zurück, oder legt ihn fest.
        /// </summary>
        public ZyanComponentHost ComponentHost { get; set; }

        /// <summary>
        /// Gibt den lokalen Komponenten-Katalog zurück.
        /// </summary>
        public ComponentCatalog LocalComponents { get; private set; }

        /// <summary>
        /// Gibt den veröffentlichten Komponenten-Katalog zurück.
        /// </summary>
        public ComponentCatalog PublishedComponents { get; private set; }

        /// <summary>
        /// Gibt eine Liste der geladenen Module zurück.
        /// </summary>
        public List<ModuleHost> Modules { get; private set; }

        /// <summary>
        /// Gibt das Bereitstellungsverzeichnis der Anwendung zurück.
        /// </summary>
        public string DeploymentDirectory { get; private set; }

        /// <summary>
        /// Erstellt eine neue Instanz von ZyanApplication.
        /// </summary>
        /// <param name="applicationName">Anwendungsname</param>        
        public ZyanApplication(string applicationName)
            : this(applicationName, AppDomain.CurrentDomain.BaseDirectory, null)
        { }

        /// <summary>
        /// Erstellt eine neue Instanz von ZyanApplication.
        /// </summary>
        /// <param name="applicationName">Anwendungsname</param>
        /// <param name="deploymentDir">Bereitstellungsverzeichnis der Anwendung</param>        
        public ZyanApplication(string applicationName, string deploymentDir)
            : this(applicationName, deploymentDir, null)
        { }

        /// <summary>
        /// Erstellt eine neue Instanz von ZyanApplication.
        /// </summary>
        /// <param name="applicationName">Anwendungsname</param>        
        /// <param name="componentHost">Komponenten Host</param>
        public ZyanApplication(string applicationName, ZyanComponentHost componentHost)
            : this(applicationName, AppDomain.CurrentDomain.BaseDirectory, componentHost)
        { }

        /// <summary>
        /// Erstellt eine neue Instanz von ZyanApplication.
        /// </summary>
        /// <param name="applicationName">Anwendungsname</param>
        /// <param name="deploymentDir">Bereitstellungsverzeichnis der Anwendung</param>
        /// <param name="componentHost">Komponenten Host</param>
        public ZyanApplication(string applicationName, string deploymentDir, ZyanComponentHost componentHost)
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

            // Veröffentlichten Komponenten-Katalog erzeugen
            PublishedComponents = new ComponentCatalog();

            // Wenn ein Komponenten Host angegeben wurde ...
            if (componentHost != null)
            { 
                // Host übernehmen
                ComponentHost = componentHost;

                // Veröffentlichenden Komponenten-Katalog des Hosts festlegen
                ComponentHost.ComponentCatalog = PublishedComponents;
            }
            // Anwendung der Anwendungs-Auflistung zufügen
            _applications.Add(applicationName, this);
        }

        // Sperrobjekt für das Laden von Modulen
        private object _loadModulesLockObject = new object();

        /// <summary>
        /// Lädt die Module der Anwendung.
        /// </summary>        
        public void LoadModules()
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
                        ModuleHost host = new ModuleHost(this, moduleDir);
                        
                        // Modul Host der Modul Host-Auflistung zufügen
                        Modules.Add(host);
                    }
                }
            }
        }

        /// <summary>
        /// Gibt zurück, ob die Anwendung läuft.
        /// </summary>
        public bool IsRunning { get; private set; }

        // Sperrobjekt für die IsRunning-Abfrage
        private object _isRunningLockObject = new object();

        /// <summary>
        /// Startet die Anwendung.
        /// </summary>
        public void Start()
        {
            lock (_isRunningLockObject)
            {
                // Wenn die Anwendung noch nicht läuft ...
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
        /// Stoppt die Anwendung.
        /// </summary>
        public void Stop()
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

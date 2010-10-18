using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Zyan.Communication.Modularity
{
    /// <summary>
    /// Laufzeitumgebaung für ein einzelnes zusammensteckbares Zyan-Modul.
    /// </summary>
    public class ModuleHost 
    {
        /// <summary>
        /// Gibt den Pfad zum Modulverzeichnis zurück, oder legt ihn fest.
        /// </summary>
        public string ModuleDirectory { get; private set; }

        /// <summary>
        /// Gibt den Modulnamen zurück, oder legt ihn fest.
        /// </summary>
        public string ModuleName { get; private set; }

        /// <summary>
        /// Gibt den Modul-Kontroller zurück, oder legt ihn fest.
        /// </summary>
        private IModuleControler ModuleControler { get; set; }

        /// <summary>
        /// Gibt das Anwendungs-Stammobjekt zurück, oder legt es fest.
        /// </summary>
        public ZyanApplication Application { get; private set; }

        /// <summary>
        /// Erzeugt eine neue Instanz von ModuleHost.
        /// </summary>
        /// <param name="application">Anwendungs-Stammobjekt</param>
        /// <param name="moduleDirectory">Absoulter Pfad zum Modulverzeichnis</param>
        public ModuleHost(ZyanApplication application, string moduleDirectory)
        {
            // Wenn kein Anwendungs-Stammobjekt übergeben wurde ...
            if (application == null)
                // Ausnahme werfen
                throw new ArgumentNullException("application");

            // Wenn kein Modulverzeichnis angegeben wurde ...
            if (string.IsNullOrEmpty(moduleDirectory))
                // Ausnahme werfen
                throw new ArgumentException();

            // Wenn das angegebene Modulverzeichnis nicht existiert ...
            if (!Directory.Exists(moduleDirectory))
                // Ausnahme werfen
                throw new DirectoryNotFoundException(moduleDirectory);

            // Werte übernehmen
            Application = application;
            ModuleDirectory=moduleDirectory;
            
            // Ordnername des Modulverzeichnisses als Modulname übernehmen
            string[] moduleNameParts = ModuleDirectory.Split('\\');
            ModuleName = moduleNameParts[moduleNameParts.Length - 1];

            // Assembly-Dateinamen (*.dll) des Modulverzeichnisses abrufen
            string[] assemblyFiles = Directory.GetFiles(ModuleDirectory,"*.dll",SearchOption.TopDirectoryOnly);
            
            // AssemblyResolve-Ereignis abonnieren
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            // Dateiname der aktuellen Assembly abrufen
            string zyanAssemblyName=Path.GetFileName(Assembly.GetExecutingAssembly().Location);

            try
            {
                // Alle Assembly-Dateinamen durchlaufen
                foreach (string assemblyFile in assemblyFiles)
                {
                    // Wenn die Assembly nicht das Zyan-Framework selbst ist ...
                    if (!Path.GetFileName(assemblyFile).Equals(zyanAssemblyName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Aktuelle Assembly laden
                        Assembly assembly = Assembly.Load(Path.GetFileNameWithoutExtension(assemblyFile));
                        
                        // Typen der Assembly abfragen
                        Type[] types = assembly.GetExportedTypes();

                        // Liste für gefundene Modul-Kontroller-Klassen erzeugen
                        List<Type> foundControlerTypes = new List<Type>();

                        // Alle Typen durchlaufen
                        foreach (Type type in types)
                        {
                            // Wenn der aktuelle Typ mit dem ZyanModuleControler-Attribut gekennzeichnet ist ...
                            if (type.GetCustomAttributes(typeof(ZyanModuleControlerAttribute), false).Count() == 1)
                                // Typ zur Liste der gefundenen Modul-Kontroller hinzufügen
                                foundControlerTypes.Add(type);
                        }
                        // Wenn genau ein Modul-Kontroller gefunden wurde ...
                        if (foundControlerTypes.Count == 1)
                        {
                            // Wenn bereits ein Modul-Kontroller gesetzt wurde ...
                            if (ModuleControler != null)
                                // Ausnahme werfen
                                throw new ApplicationException("Mehrere Modulcontroller im selben Modul sind nicht zulässig.");

                            // Modulkontroler für dieses Modul festlegen
                            ModuleControler = Activator.CreateInstance(foundControlerTypes.First()) as IModuleControler;
                        }
                        else if (foundControlerTypes.Count > 0) // Wenn mehrere Modul-Kontroller gefunden wurden ...
                            // Ausnahme werfen
                            throw new ApplicationException("Mehrere Modulcontroller im selben Modul sind nicht zulässig.");
                    }
                }
            }
            finally
            {
                // AssemblyResolve-Ereignis abbestellen
                AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            }
        }

        /// <summary>
        /// Ereignisprozedur: Beim Auflösen einer Assembly.
        /// </summary>
        /// <param name="sender">Herkunftsobjekt</param>
        /// <param name="e">Ereignisargumente</param>
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Alle Assembly-Dateien im Modulverzeichnis und dessen Unterverzeichnissen suchen
            string[] files = Directory.GetFiles(ModuleDirectory, "*.dll", SearchOption.AllDirectories);

            // Alle Assembly-Dateien durchlaufen
            foreach (string file in files)
            {
                // Wenn die aktuelle Assembly-Datei die gesuchte Assembly ist ...
                if ((Path.GetFileNameWithoutExtension(file) == args.Name.Split(',')[0].Trim()) || (Path.GetFileNameWithoutExtension(file)==args.Name) || (Path.GetFileName(file)==args.Name))
                {
                    // Assembly laden
                    Assembly assembly = Assembly.LoadFrom(file);
                    
                    // Assembly zurückgeben
                    return assembly;
                }        
            }
            // null zurückgeben
            return null;
        }

        /// <summary>
        /// Startet das Modul.
        /// </summary>
        public void StartModule()
        {
            // Wenn ein Modul-Kontroller festgelgt ist ...
            if (ModuleControler!=null)
                // OnStart-Methode des Modul-Kontrollers aufrufen
                ModuleControler.OnStart(Application);
        }

        /// <summary>
        /// Stoppt das Modul.
        /// </summary>
        public void StopModule()
        {
            // Wenn ein Modul-Kontroller festgelgt ist ...
            if (ModuleControler != null)
                // OnStop-Methode des Modul-Kontrollers aufrufen
                ModuleControler.OnStop();
        }
    }
}

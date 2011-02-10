using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Zyan.Communication.Protocols;

namespace Zyan.Communication
{
    /// <summary>
    /// Beschreibt Konfigurationseinstellungen für eine einzelne Zyan-Verbindung.
    /// </summary>
    [Serializable]
    public class ZyanConnectionSetup
    {
        /// <summary>
        /// Ersellt eine neue Instanz von ZyanConnectionSetup.
        /// </summary>
        public ZyanConnectionSetup()
        {
            // Auflistung für Anmeldeinformationen erzeugen
            Credentials = new Hashtable();

            // Standardwerte festlegen
            AutoLoginOnExpiredSession = false;
            KeepSessionAlive = true;
        }

        /// <summary>
        /// Gibt den Server-URL (z.B. "tcp://server1:46123/host1") zurück oder legt ihn fest.
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// Gibt die Protokollkonfiguration zurück oder legt sie fest.
        /// </summary>
        public IClientProtocolSetup ProtocolSetup { get; set; }
        
        /// <summary>
        /// Gibt die Anmeldeinformationen zurück, oder legt sie fest.
        /// </summary>
        public Hashtable Credentials { get; set; }
        
        /// <summary>
        /// Gibt zurück, ob sich die Verbindung automatisch neu anmelden soll, wenn die Sitzung abgelaufen ist, oder legt diest fest.
        /// </summary>
        public bool AutoLoginOnExpiredSession { get; set; }
        
        /// <summary>
        /// Gibt zurück, ob die Sitzung automatisch turnusgemäß verlängert werden soll, oder legt dies fest.
        /// </summary>
        public bool KeepSessionAlive { get; set; }

        /// <summary>
        /// Fügt ein neue Anmeldeinformation hinzu.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="value">Wert</param>
        public void AddCredential(string name, string value)
        { 
            // Wenn noch keine Auflistung für Anmeldeinformationen existiert ...
            if (Credentials == null)
                // Neue Auflistung erzeugen
                Credentials = new Hashtable();

            // Name-Wert-Paar hinzufügen
            Credentials.Add(name, value);
        }
    }
}

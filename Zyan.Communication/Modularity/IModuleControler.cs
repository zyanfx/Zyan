using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Modularity
{
    /// <summary>
    /// Schnittstelle für Modul-Kontroller.
    /// </summary>
    public interface IModuleControler
    {
        /// <summary>
        /// Wird beim Starten vom Modul Host aufgerufen.
        /// </summary>        
        void OnStart();

        /// <summary>
        /// Wird beim Stoppen vom Modul Host aufgerufen.
        /// </summary>
        void OnStop();
    }
}

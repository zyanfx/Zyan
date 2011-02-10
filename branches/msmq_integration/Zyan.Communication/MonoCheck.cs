using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication
{
    /// <summary>
    /// Werkzeug zur Prüfung auf mono Laufzeitumgebung.
    /// </summary>
    internal static class MonoCheck
    {
        // Felder
        private static bool _runtimeChecked = false;
        private static bool _runningOnMono = false;

        /// <summary>
        /// Gibt zurück, ob Zyan mit mono ausgewführt wird, oder nicht.
        /// </summary>
        public static bool IsRunningOnMono
        {
            get
            {
                // Wenn die Laufzeitumgtebung noch nicht geprüft wurde ...
                if (!_runtimeChecked)
                    // Prüfen, ob der Typ "Mono.Runtime" aufgelöst werden kann
                    _runningOnMono = Type.GetType("Mono.Runtime") != null;

                // Wahr zurückgeben, wenn Zyan mit mono ausgeführt wird, ansonsten Falsch
                return _runningOnMono;
            }
        }
    }
}

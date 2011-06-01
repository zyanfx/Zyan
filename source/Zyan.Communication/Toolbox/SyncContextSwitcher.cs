using System;
using System.Threading;

namespace Zyan.Communication.Toolbox
{
    /// <summary>
    /// Stellt sicher, dass die Verarbeitung von Nachrichten im ursprünglichen Thread stattfinden.
    /// </summary>    
    public class SyncContextSwitcher <T>
    {
        // Bei Erstellung den aktuellen Synchronisierungskontext merken
        private readonly SynchronizationContext syncContext = SynchronizationContext.Current;

        /// <summary>
        /// Aktion, die ausgeführt wird, wenn eine Nachricht verarbeitet werden soll.
        /// </summary>
        public Action<T> Out;

        /// <summary>
        /// Verarbeitet eine Nachricht und berücksichtigt dabei den Synchronisierungskontext.
        /// </summary>
        /// <param name="message">Nachricht</param>
        public void In(T message)
        { 
            // Wenn der Aufruf aus einem anderen thread stammt ...
            if (syncContext != null)
                // Aufruf an ursprünglichen Thread senden
                syncContext.Send(x => this.Out(message), null);
            else
                // Aufruf direkt ausführen
                Out(message);
        }

        /// <summary>
        /// Erstellt eine neue Instanz und verdrahtet damit zwei Pins.
        /// </summary>
        /// <param name="inputPin">Eingangs-Pin</param>
        /// <returns>Ausgangs-Pin</returns>
        public static Action<T> WireUp(Action<T> inputPin)
        {
            // Neue Instanz erzeugen
            SyncContextSwitcher<T> instance = new SyncContextSwitcher<T>();
           
            // Eingangs-Pin mit Ausgangs-Pin der Instanz verdrahten
            instance.Out = inputPin;

            // Delegat auf Eingangs-Pin der Instanz zurückgeben
            return new Action<T>(instance.In);
        }
    }
}

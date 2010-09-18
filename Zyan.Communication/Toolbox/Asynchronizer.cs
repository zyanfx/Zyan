using System;
using System.Threading;

namespace Zyan.Communication.Toolbox    
{
    /// <summary>
    /// Führt die Verarbeitung einer Nachricht asynchron aus.
    /// </summary>
    /// <typeparam name="T">Nachrichtentyp</typeparam>
    public class Asynchronizer<T> 
    {
        /// <summary>
        /// Aktion, die zur asynchronen Verarbeitung der Nachricht aufgerufen wird.
        /// </summary>
        public Action<T> Out;

        /// <summary>
        /// Bestimmte Nachricht mit der festgelegten Aktion asychron verarbeiten.
        /// </summary>
        /// <param name="message"></param>
        public void In(T message)
        { 
            // Verarbeitung in neuem Thread starten
            ThreadPool.QueueUserWorkItem(x=>this.Out(message));
        }
    }
}

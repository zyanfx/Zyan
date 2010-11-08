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
        public Action<T> Out { get; set; }

        /// <summary>
        /// Bestimmte Nachricht mit der festgelegten Aktion asychron verarbeiten.
        /// </summary>
        /// <param name="message"></param>
        public void In(T message)
        { 
            // Verarbeitung in neuem Thread starten
            ThreadPool.QueueUserWorkItem(x=>this.Out(message));
        }

        public static Action<T> WireUp(Action<T> inputPin)
        {
            Asynchronizer<T> instance = new Asynchronizer<T>();
            instance.Out=inputPin;
            return new Action<T>(instance.In);
        }
    }

    /// <summary>
    /// Führt die Verarbeitung einer Nachricht asynchron aus.
    /// </summary>    
    public class Asynchronizer
    {
        /// <summary>
        /// Aktion, die zur asynchronen Verarbeitung der Nachricht aufgerufen wird.
        /// </summary>
        public dynamic Out { get; set; }

        /// <summary>
        /// Bestimmte Nachricht mit der festgelegten Aktion asychron verarbeiten.
        /// </summary>
        /// <param name="message"></param>
        public void In(dynamic message)
        {
            // Verarbeitung in neuem Thread starten
            ThreadPool.QueueUserWorkItem(x => this.Out(message));
        }

        public static Action<dynamic> WireUp(dynamic inputPin)
        {
            Asynchronizer instance = new Asynchronizer();
            instance.Out = inputPin;
            return new Action<dynamic>(instance.In);
        }
    }
}

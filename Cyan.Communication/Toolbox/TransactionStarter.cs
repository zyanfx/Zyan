using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace Cyan.Communication.Toolbox
{
    /// <summary>
    /// Stellt sicher, dass die durchlaufende Nachricht innerhalb einer Transaktion verarbeitet wird.
    /// </summary>
    /// <typeparam name="T">Nachichtentyp</typeparam>
    public class TransactionStarter<T>
    {
        /// <summary>
        /// Erzeugt eine neue Instanz von TransactionStarter<T>.
        /// </summary>
        public TransactionStarter()
        {
            // Standardwerte setzen
            IsolationLevel = IsolationLevel.ReadCommitted;
            ScopeOption = TransactionScopeOption.Required;
            Timeout = new TimeSpan(0, 0, 30);
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TransactionStarter<T>.
        /// </summary>
        /// <param name="isolationLevel">Isolationsstufe</param>
        /// <param name="scopeOption">bereichsoption</param>
        /// <param name="timeout">Ablaufzeitspanne</param>
        public TransactionStarter(IsolationLevel isolationLevel,TransactionScopeOption scopeOption, TimeSpan timeout)
        {
            // Felder füllen
            IsolationLevel = isolationLevel;
            ScopeOption = scopeOption;
            Timeout = timeout;
        }

        /// <summary>
        /// Gibt die Isolationsstufe der Transaktion zurück, oder legt sie fest.
        /// </summary>
        public IsolationLevel IsolationLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt die Ablaufzeitspanne zurück oder legt sie fest.
        /// </summary>
        public TimeSpan Timeout
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt die Bereichsoption zurück oder legt sie fest.
        /// </summary>
        public TransactionScopeOption ScopeOption
        {
            get;
            set;
        }

        /// <summary>
        /// Eingangs-Pin.
        /// </summary>
        /// <param name="message">Nachricht</param>
        public void In(T message)
        { 
            // Transaktionsoptionen setzen
            TransactionOptions options=new TransactionOptions();
            options.IsolationLevel=IsolationLevel;
            options.Timeout=Timeout;

            try
            {
                // Transaktionsbereich erzeugen
                using (TransactionScope scope = new TransactionScope(ScopeOption, options))
                {
                    // Nachricht an Ausgangs-Pin übergeben
                    Out(message);

                    // Transaktion abschließen
                    scope.Complete();
                }
            }
            catch (TransactionAbortedException)
            { 
                // Wenn der Transaktionsabbruch-Pin verdrahtet ist ...
                if (Out_TransactionAborted != null)
                    // Pin aufrufen
                    Out_TransactionAborted();
            }
        }

        /// <summary>
        /// Ausgangs-Pin.
        /// </summary>
        public Action<T> Out;

        /// <summary>
        /// Ausgangs-Pin, bei Transaktionsabbruch.
        /// </summary>
        public Action Out_TransactionAborted;
    }
}

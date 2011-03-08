using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication
{
    /// <summary>
    /// Auflistung von Aufrufabfangvorrichtungen.
    /// </summary>
    public class CallInterceptorCollection : Collection<CallInterceptor>
    {
        // Sperrobjekt (für Threadsync.)
        private object _lockObject = new object();

        /// <summary>
        /// Erzeugt eine neue Instanz der CallInterceptorCollection-Klasse.
        /// </summary>
        internal CallInterceptorCollection() : base()
        {}

        /// <summary>
        /// Wird aufgerufen, wenn ein neuer Eintrag eingefügt wird.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="item">Objekt</param>
        protected override void InsertItem(int index, CallInterceptor item)
        {
            lock (_lockObject)
            {
                base.InsertItem(index, item);
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn ein Eintrag entfernt wird.
        /// </summary>
        /// <param name="index">Index</param>
        protected override void RemoveItem(int index)
        {
            lock (_lockObject)
            {
                base.RemoveItem(index);
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn ein Eintrag neu zugewiesen wird.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="item">Objekt</param>
        protected override void SetItem(int index, CallInterceptor item)
        {
            lock (_lockObject)
            {
                base.SetItem(index, item);
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn alle Einträge entfernt werden sollen.
        /// </summary>
        protected override void ClearItems()
        {
            lock (_lockObject)
            {
                base.ClearItems();
            }
        }

        /// <summary>
        /// Sucht eine passende Aufrufabfangvorrichtung für ein bestimmten Methodenaufruf.
        /// </summary>
        /// <param name="interfaceType">Typ der Dienstschnittstelle</param>
        /// <param name="remotingMessage">Remoting-Nachricht des Methodenaufrufs vom Proxy</param>
        /// <returns>Aufrufabfangvorrichtung oder null</returns>
        public CallInterceptor FindMatchingInterceptor(Type interfaceType, IMethodCallMessage remotingMessage)
        {
            // Wenn keine Abfangvorrichtungen registriert sind ...
            if (Count == 0)
                // null zurückgeben
                return null;

            // Passende Aufrufabfangvorrichtung suchen und zurückgeben
            return (from interceptor in this
                    where interceptor.InterfaceType.Equals(interfaceType) &&
                          interceptor.MemberType == remotingMessage.MethodBase.MemberType &&
                          interceptor.MemberName == remotingMessage.MethodName &&
                          string.Join("|", (from paramType in interceptor.ParameterTypes select paramType.FullName).ToArray()) == string.Join("|", (from paramType2 in remotingMessage.MethodBase.GetParameters() select paramType2.ParameterType.FullName).ToArray())
                    select interceptor).FirstOrDefault();
        }
    }
}

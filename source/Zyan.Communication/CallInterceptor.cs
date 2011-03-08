using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Zyan.Communication
{
    /// <summary>
    /// Delegat für benutzerdefinierte Aufrufabfanglogik.
    /// </summary>
    /// <param name="action">Aufrufabfang-Aktion</param>
    public delegate void CallInterceptionDelegate(CallInterceptionData action);
        
    /// <summary>
    /// Allgemeine Implementierung einer Aufrufabfangvorrichtung.
    /// </summary>
    public class CallInterceptor
    {
        /// <summary>
        /// Erzeugt eine neue Instanz der CallInterceptor-Klasse.
        /// </summary>
        /// <param name="interfaceType">Schnittstellentyp der Komponente, deren Aufrufe abgefangen werden sollen</param>
        /// <param name="memberType">Art des Members, dessen Aufrufe abgefangen werden sollen</param>
        /// <param name="memberName">Name des Members, dessen Aufrufe abgefangen werden sollen</param>
        /// <param name="parameterTypes">Array mit den Typen der Parameter des abzufangenden Members</param>
        /// <param name="onInterception">Delegat, der beim Abfangen aufgerufen wird</param>
        public CallInterceptor(Type interfaceType,MemberTypes memberType, string memberName, Type[] parameterTypes, CallInterceptionDelegate onInterception)
        {
            // Eigenschaften füllen
            InterfaceType = interfaceType;
            MemberType = memberType;
            MemberName = memberName;
            ParameterTypes = parameterTypes;
            OnInterception = onInterception;
        }

        /// <summary>
        /// Gibt die Schnittstelle der Komponenten zurück, deren Aufruf abgefangen werden soll, oder legt sie fest.
        /// </summary>
        public Type InterfaceType { get; private set; }
        
        /// <summary>
        /// Gibt die Art des abzufangenden Members zurück, oder legt ihn fest.
        /// </summary>
        public MemberTypes MemberType { get; private set; }

        /// <summary>
        /// Gibt den Namen der abzufangenden Methode zurück, oder legt ihn fest.
        /// </summary>
        public string MemberName { get; private set; }

        /// <summary>
        /// Gibt ein Array der als Parameter erwarteten Typen der aubzufangenden Methode zurück, oder legt sie fest.
        /// </summary>
        public Type[] ParameterTypes { get; private set; }

        /// <summary>
        /// Gibt den Delegaten zurück, der beim Abfangen des Aufrufs anstelle dessen aufgerufen wird, oder legt ihn fest.
        /// </summary>
        public CallInterceptionDelegate OnInterception { get; private set; }
    }
}

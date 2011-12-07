using System;
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
		/// <param name="uniqueName">Unique name of the intercepted component.</param>
		/// <param name="memberType">Art des Members, dessen Aufrufe abgefangen werden sollen</param>
		/// <param name="memberName">Name des Members, dessen Aufrufe abgefangen werden sollen</param>
		/// <param name="parameterTypes">Array mit den Typen der Parameter des abzufangenden Members</param>
		/// <param name="onInterception">Delegat, der beim Abfangen aufgerufen wird</param>
		public CallInterceptor(Type interfaceType, string uniqueName, MemberTypes memberType, string memberName, Type[] parameterTypes, CallInterceptionDelegate onInterception)
		{
			// Eigenschaften füllen
			InterfaceType = interfaceType;
			UniqueName = string.IsNullOrEmpty(uniqueName) ? interfaceType.FullName : uniqueName;
			MemberType = memberType;
			MemberName = memberName;
			ParameterTypes = parameterTypes;
			OnInterception = onInterception;
			Enabled = true;
		}

		/// <summary>
		/// Erzeugt eine neue Instanz der CallInterceptor-Klasse.
		/// </summary>
		/// <param name="interfaceType">Schnittstellentyp der Komponente, deren Aufrufe abgefangen werden sollen</param>
		/// <param name="memberType">Art des Members, dessen Aufrufe abgefangen werden sollen</param>
		/// <param name="memberName">Name des Members, dessen Aufrufe abgefangen werden sollen</param>
		/// <param name="parameterTypes">Array mit den Typen der Parameter des abzufangenden Members</param>
		/// <param name="onInterception">Delegat, der beim Abfangen aufgerufen wird</param>
		public CallInterceptor(Type interfaceType, MemberTypes memberType, string memberName, Type[] parameterTypes, CallInterceptionDelegate onInterception)
			: this(interfaceType, null, memberType, memberName, parameterTypes, onInterception)
		{
		}

		/// <summary>
		/// Gibt die Schnittstelle der Komponenten zurück, deren Aufruf abgefangen werden soll, oder legt sie fest.
		/// </summary>
		public Type InterfaceType { get; private set; }

		/// <summary>
		/// Gets the unique name of intercepted component.
		/// </summary>
		public string UniqueName { get; private set; }

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

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="CallInterceptor"/> is enabled.
		/// </summary>
		/// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
		public bool Enabled { get; set; }
	}
}

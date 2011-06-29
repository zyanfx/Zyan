using System;
using System.Collections.Generic;

namespace Zyan.Communication
{
	/// <summary>
	/// Beschreibt Ereignisargumente für Aufrufereignisse.
	/// </summary>
	public class AfterInvokeEventArgs : EventArgs
	{
		/// <summary>
		/// Gibt den Nachverfolgungsschlüssel des Methodenaufrufs zurück, oder legt ihn fest.
		/// </summary>
		public Guid TrackingID { get; set; }

		/// <summary>
		/// Gibt den Namen der Komponentenschnittstelle zurück, oder legt ihn fest.
		/// </summary>
		public string InterfaceName { get; set; }

		/// <summary>
		/// Gibt den Korrelationssatz für Delegaten zurück, oder legt ihn fest.
		/// </summary>
		public List<DelegateCorrelationInfo> DelegateCorrelationSet { get; set; }

		/// <summary>
		/// Gibt den Methodennamen zurück, oder legt ihn fest.
		/// </summary>
		public string MethodName { get; set; }

		/// <summary>
		/// Gibt die Methodenargumente zurück, oder legt sie fest.
		/// </summary>
		public object[] Arguments { get; set; }

		/// <summary>
		/// Gibt den Rückgabewert der Methode zurück, oder legt ihn fest.
		/// </summary>
		public object ReturnValue { get; set; }

		/// <summary>
		/// Gibt den Inhalt des Objekts als Zeichenkette ausgedrückt zurück.
		/// </summary>
		/// <returns>Zeichenkette</returns>
		public override string ToString()
		{
			// Auflistung für String-Repräsentation der Argumente erzeugen
			List<string> argsAsString = new List<string>();

			// Wenn Argumente vorhanden sind ...
			if (Arguments != null)
			{
				// Alle Argumente durchlaufen
				foreach (object arg in Arguments)
				{
					// Wenn das aktuelle Argument null ist ...
					if (arg == null)
						// "null" in Auflistung schreiben
						argsAsString.Add("null");
					else
						// ToString-Ausgabe in Auflistung schreiben
						argsAsString.Add(arg.ToString());
				}
			}
			// Argumentkette aufbauen
			string argChain = string.Join(", ", argsAsString.ToArray());

			// Wenn der Rückgabewert nicht null ist ...
			if (ReturnValue != null)
				// Aufruf als Zeichenkette formatieren und zurückgeben
				return string.Format("{0}.{1}({2}) = {3}", InterfaceName, MethodName, argChain, ReturnValue.ToString());
			else
				// Aufruf als Zeichenkette formatieren und zurückgeben
				return string.Format("{0}.{1}({2}) = null", InterfaceName, MethodName, argChain);
		}
	}
}

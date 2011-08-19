using System;
using System.Collections.Generic;
using Zyan.Communication.Delegates;

namespace Zyan.Communication
{
	/// <summary>
	/// Beschreibt Ereignisargumente für Aufrufereignisse mit Abbruchmöglichkeit.
	/// </summary>
	public class BeforeInvokeEventArgs : EventArgs
	{
		/// <summary>
		/// Gibt den Nachverfolgungsschlüssel des Methodenaufrufs zurück, oder legt ihn fest.
		/// </summary>
		public Guid TrackingID { get; set; }

		/// <summary>
		/// Gibt zurück, ob der Aufruf abgebrochen werden soll, oder legt diest fest.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Gibt die Ausnahme für den Abbruch zurück, oder legt sie fest.
		/// </summary>
		public InvokeCanceledException CancelException { get; set; }

		/// <summary>
		/// Gibt den Namen der Komponentenschnittstelle zurück, oder legt ihn fest.
		/// </summary>
		public string InterfaceName { get; set; }

		/// <summary>
		/// Gibt den Korrelationssatz für Ausgangs-Pins zurück, oder legt ihn fest.
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

			// Aufruf als Zeichenkette formatieren und zurückgeben
			return string.Format("{0}.{1}({2})", InterfaceName, MethodName, argChain);
		}
	}
}

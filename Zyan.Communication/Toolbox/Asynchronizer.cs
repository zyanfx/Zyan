using System;
using System.Threading;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Führt die Verarbeitung einer Nachricht asynchron aus.
	/// </summary>    
	public class Asynchronizer<T>
	{
		/// <summary>
		/// Aktion, die zur asynchronen Verarbeitung der Nachricht aufgerufen wird.
		/// </summary>
		public Action<T> Out { get; set; }

		/// <summary>
		/// Bestimmte Nachricht mit der festgelegten Aktion asychron verarbeiten.
		/// </summary>
		/// <param name="message">Nachricht</param>
		public void In(T message)
		{
			// Verarbeitung in neuem Thread starten
			ThreadPool.QueueUserWorkItem(x => this.Out(message));
		}

		/// <summary>
		/// Erstellt eine neue Instanz und verdrahtet damit zwei Pins.
		/// </summary>
		/// <param name="inputPin">Eingangs-Pin</param>
		/// <returns>Ausgangs-Pin</returns>
		public static Action<T> WireUp(Action<T> inputPin)
		{
			// Neue Instanz erzeugen
			Asynchronizer<T> instance = new Asynchronizer<T>();

			// Eingangs-Pin mit Ausgangs-Pin der Instanz verdrahten
			instance.Out = inputPin;

			// Delegat auf Eingangs-Pin der Instanz zurückgeben
			return new Action<T>(instance.In);
		}
	}
}

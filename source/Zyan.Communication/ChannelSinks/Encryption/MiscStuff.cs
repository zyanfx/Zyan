using System;
using System.Security.Cryptography;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
	/// Namen der von Client und Server gemeinsam genutzten Transportheader.
	/// </summary>
	internal class CommonHeaderNames
	{
		/// <summary>
		/// Eindeutige Kennung der Sicherheitstransaktion.
		/// </summary>
		public const string SECURE_TRANSACTION_ID = "X-CY_SECURE_TRANSACTION_ID";

		/// <summary>
		/// Status der Sicherheitstransaktion.
		/// </summary>
		public const string SECURE_TRANSACTION_STATE = "X-CY_SECURE_TRANSACTION_STATE";

		/// <summary>
		/// Öffentlicher RSA Schlüssel.
		/// </summary>
		public const string PUBLIC_KEY = "X-CY_PUBLIC_KEY";

		/// <summary>
		/// Verschlüsselter gemeinsamer Schlüssel.
		/// </summary>
		public const string SHARED_KEY = "X-CY_SHARED_KEY";

		/// <summary>
		/// Verschlüsselter gemeinsamer Inizialisierungsvektor.
		/// </summary>
		public const string SHARED_IV = "X-CY_SHARED_IV";
	}

	/// <summary>
	/// Aufzählung der einzelnen Verarbeitungsschritte einer Sicherheitstransaktion.
	/// </summary>
	internal enum SecureTransactionStage
	{
		/// <summary>
		/// Uninizialisiert, noch nichts geschehen.
		/// </summary>
		Uninitialized = 0,

		/// <summary>
		/// Client sendet den öffentlichen Schlüssel an den Server.
		/// </summary>
		SendingPublicKey,

		/// <summary>
		/// Server sendet den verschlüsselten gemeinsamen Schlüssel zurück zum Client.
		/// </summary>
		SendingSharedKey,

		/// <summary>
		/// Client sendet die verschlüsselte Anfragenachricht an den Server.
		/// </summary>
		SendingEncryptedMessage,

		/// <summary>
		/// Server sendet die verschlüsselte Antwortnachricht an den Client zurück.
		/// </summary>
		SendingEncryptedResult,

		/// <summary>
		/// Unbekannte Sicherheitstransaktionskennung.
		/// </summary>
		UnknownTransactionID
	}

	/// <summary>
	/// Enthält Clientverbindungsinformation zu einer Sicherheitstransaktion.
	/// </summary>
	internal class ClientConnectionData : IDisposable
	{
		#region Deklarationen

		// Eindeutige Sicherheitstransaktionskennung des Clients
		private Guid _secureTransactionID;

		// Kryptografieanbieter für symmetrische Verschlüsselung
		private SymmetricAlgorithm _cryptoProvider;

		// Zeitpunkt der letzten Kommunikation
		private DateTime _timestamp;

		// Gibt an, ob das Objekt bereits entsorgt wurde
		private bool _disposed = false;

		// When reentrancy level is greater than zero, it means that the server call is in progress
		private int reentrancyLevel;

		#endregion

		#region Konstruktor und Desktruktor

		/// <summary>Erstellt eine neue Instanz von ClientConnectionData</summary>
		/// <param name="secureTransactionID">Sicherheitstransaktionskennung</param>
		/// <param name="cryptoProvider">Verschlüsselungsanbieter</param>
		public ClientConnectionData(Guid secureTransactionID, SymmetricAlgorithm cryptoProvider)
		{
			// Wenn kein Kryptografieanbieter übergeben wurde ...
			if (cryptoProvider == null)
				// Ausnahme werfen
				throw new ArgumentNullException("cryptoProvider");

			// Werte übernehmen
			_secureTransactionID = secureTransactionID;
			_cryptoProvider = cryptoProvider;
			_timestamp = DateTime.UtcNow;
		}

		/// <summary>
		/// Entsorgt die Verbindungsdaten.
		/// </summary>
		~ClientConnectionData()
		{
			// Verwaltete Ressourcen freigeben
			Dispose(false);
		}

		#endregion

		#region Methoden

		/// <summary>
		/// Aktualisiert den Zeitstempel.
		/// </summary>
		public void UpdateTimestamp()
		{
			// Prüfen, ob das Objekt bereits entsorgt wurde
			CheckDisposed();

			// Zeitstempel aktualisieren
			_timestamp = DateTime.UtcNow;
		}

		/// <summary>
		/// Begins the method call.
		/// </summary>
		public void BeginMethodCall()
		{
			CheckDisposed();
			System.Threading.Interlocked.Increment(ref reentrancyLevel);
		}

		/// <summary>
		/// Ends the method call.
		/// </summary>
		public void EndMethodCall()
		{
			CheckDisposed();
			System.Threading.Interlocked.Decrement(ref reentrancyLevel);
		}

		/// <summary>
		/// Gibt verwaltete Ressourcen frei.
		/// </summary>
		void IDisposable.Dispose()
		{
			// Ressourcen freigeben und sicherstellen, dass der Destruktor nicht aufgerufen wird
			Dispose(true);
		}

		/// <summary>
		/// Gibt verwaltete Ressourcen frei.
		/// </summary>
		/// <param name="disposing">Legt fest, ob der Destruktor nicht aufgerufen werden soll</param>
		protected void Dispose(bool disposing)
		{
			// Wenn das Objekt nicht bereits entsorgt wurde ...
			if (!_disposed)
			{
				// Wenn der Kryptografieanbieter noch existiert ...
				if (_cryptoProvider != null)
					// Kryptografieanbieter entsorgen
					((IDisposable)_cryptoProvider).Dispose();

				// Wenn der Destruktor nicht mehr aufgerufen werden soll ...
				if (disposing)
					// Aufruf des Konstrukturs durch den Müllsamler unterbinden
					GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Wirft eine Ausnahme, wenn das Objekt bereits entsorgt wurde.
		/// </summary>
		private void CheckDisposed()
		{
			// Wenn das Objekt bereits entsorgt wurde ...
			if (_disposed)
				// Ausnahme werfen
				throw new ObjectDisposedException("ClientConnectionData");
		}

		#endregion

		#region Eigenschaften

		/// <summary>
		/// Gibt die Sicherheitstransaktionskennung zurück.
		/// </summary>
		public Guid SecureTransactionID
		{
			get
			{
				// Prüfen, ob das Objekt bereits entsorgt wurde
				CheckDisposed();

				// Sicherheitstransaktionskennung zurückgeben
				return _secureTransactionID;
			}
		}

		/// <summary>
		/// Gibt den verwendeten Kryptografieanbieter für symmetrische Verschlüsselung zurück.
		/// </summary>
		public SymmetricAlgorithm CryptoProvider
		{
			get
			{
				// Prüfen, ob das Objekt bereits entsorgt wurde
				CheckDisposed();

				// Kryptografieanbieter zurückgeben
				return _cryptoProvider;
			}
		}

		/// <summary>
		/// Gibt den Zeitpunkt der letzten Kommunikation zurück.
		/// </summary>
		public DateTime Timestamp
		{
			get
			{
				// Prüfen, ob das Objekt bereits entsorgt wurde
				CheckDisposed();

				// Zeitstempel zurückgeben
				return _timestamp;
			}
		}

		/// <summary>
		/// Gets a value indicating whether a call is in progress.
		/// </summary>
		public bool CallInProgress
		{
			get
			{
				CheckDisposed();
				return reentrancyLevel > 0;
			}
		}

		#endregion
	}
}

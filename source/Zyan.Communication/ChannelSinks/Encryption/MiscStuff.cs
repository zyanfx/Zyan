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
		/// �ffentlicher RSA Schl�ssel.
		/// </summary>
		public const string PUBLIC_KEY = "X-CY_PUBLIC_KEY";

		/// <summary>
		/// Verschl�sselter gemeinsamer Schl�ssel.
		/// </summary>
		public const string SHARED_KEY = "X-CY_SHARED_KEY";

		/// <summary>
		/// Verschl�sselter gemeinsamer Inizialisierungsvektor.
		/// </summary>
		public const string SHARED_IV = "X-CY_SHARED_IV";
	}

	/// <summary>
	/// Aufz�hlung der einzelnen Verarbeitungsschritte einer Sicherheitstransaktion.
	/// </summary>
	internal enum SecureTransactionStage
	{
		/// <summary>
		/// Uninizialisiert, noch nichts geschehen.
		/// </summary>
		Uninitialized = 0,

		/// <summary>
		/// Client sendet den �ffentlichen Schl�ssel an den Server.
		/// </summary>
		SendingPublicKey,

		/// <summary>
		/// Server sendet den verschl�sselten gemeinsamen Schl�ssel zur�ck zum Client.
		/// </summary>
		SendingSharedKey,

		/// <summary>
		/// Client sendet die verschl�sselte Anfragenachricht an den Server.
		/// </summary>
		SendingEncryptedMessage,

		/// <summary>
		/// Server sendet die verschl�sselte Antwortnachricht an den Client zur�ck.
		/// </summary>
		SendingEncryptedResult,

		/// <summary>
		/// Unbekannte Sicherheitstransaktionskennung.
		/// </summary>
		UnknownTransactionID
	}

	/// <summary>
	/// Enth�lt Clientverbindungsinformation zu einer Sicherheitstransaktion.
	/// </summary>
	internal class ClientConnectionData : IDisposable
	{
		#region Deklarationen

		// Eindeutige Sicherheitstransaktionskennung des Clients
		private Guid _secureTransactionID;

		// Kryptografieanbieter f�r symmetrische Verschl�sselung
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
		/// <param name="cryptoProvider">Verschl�sselungsanbieter</param>
		public ClientConnectionData(Guid secureTransactionID, SymmetricAlgorithm cryptoProvider)
		{
			// Wenn kein Kryptografieanbieter �bergeben wurde ...
			if (cryptoProvider == null)
				// Ausnahme werfen
				throw new ArgumentNullException("cryptoProvider");

			// Werte �bernehmen
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
			// Pr�fen, ob das Objekt bereits entsorgt wurde
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
					// Aufruf des Konstrukturs durch den M�llsamler unterbinden
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
		/// Gibt die Sicherheitstransaktionskennung zur�ck.
		/// </summary>
		public Guid SecureTransactionID
		{
			get
			{
				// Pr�fen, ob das Objekt bereits entsorgt wurde
				CheckDisposed();

				// Sicherheitstransaktionskennung zur�ckgeben
				return _secureTransactionID;
			}
		}

		/// <summary>
		/// Gibt den verwendeten Kryptografieanbieter f�r symmetrische Verschl�sselung zur�ck.
		/// </summary>
		public SymmetricAlgorithm CryptoProvider
		{
			get
			{
				// Pr�fen, ob das Objekt bereits entsorgt wurde
				CheckDisposed();

				// Kryptografieanbieter zur�ckgeben
				return _cryptoProvider;
			}
		}

		/// <summary>
		/// Gibt den Zeitpunkt der letzten Kommunikation zur�ck.
		/// </summary>
		public DateTime Timestamp
		{
			get
			{
				// Pr�fen, ob das Objekt bereits entsorgt wurde
				CheckDisposed();

				// Zeitstempel zur�ckgeben
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

using System;
using System.IO;
using System.Security.Cryptography;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
    /// Kryptographie-Werkzeuge.
    /// </summary>
	public static class CryptoTools
	{
		// Puffergröße in Byte
		private const int _bufferSize = 2048;	
	
		/// <summary>
        /// Erzeugt einen Kryptografieanbieter für symmetrische Verschlüsselung.         ///
        /// <remarks>
        /// Folgende Algorithmen weren unterstützt:
        /// 
        /// "DES", "3DES", "RIJNDAEL" und "RC2"
        /// </remarks>
        /// </summary>
		/// <param name="algorithm">Name des zu verwendenden Verschlüsselungsalgorithmus (z.B. "3DES")</param>
        /// <returns>Kryptografieanbieter für symmetrische Verschlüsselung</returns>		
		public static SymmetricAlgorithm CreateSymmetricCryptoProvider(string algorithm)
		{
			// Angegebenen Algorithmusnamen auswerten
			switch(algorithm.Trim().ToLower())
			{
				case "3des": // Triple-DES
                    return new TripleDESCryptoServiceProvider();
				
                case "rijndael": // Rijndael
                    return new RijndaelManaged();

				case "rc2": // RC2
                    return new RC2CryptoServiceProvider();

				case "des": // DES
                    return new DESCryptoServiceProvider();
				
                default: // Ansonsten ...
                    throw new ArgumentException(string.Format("Der angegeben Verschlüsselungsalgorithmus '{0}' wird nicht unterstützt. Bitte geben Sie einen der folgende Algorithmen an: '3DES', 'DES', 'RIJNDAEL' oder 'RC2'.",algorithm), "algorithm");
			}
		}
        
		/// <summary>
		/// Verschlüsselt einen bestimmten Datenstrom symmetrisch und gibt den verschlüsselten Datenstrom zurück.
        /// <remarks>
        /// Der verschlüsselte Datenstrom wird automatisch auf Position 0 zurückgesetzt.
        /// </remarks>
		/// </summary>
		/// <param name="inputStream">Unverschlüsselter Eingabedatenstrom</param>
        /// <param name="provider">Kryptografieanbieter für symmetrische Verschlüsselung</param>
		/// <returns>Verschlüsselter Datenstrom</returns>
		public static Stream GetEncryptedStream(Stream inputStream, SymmetricAlgorithm provider) 
		{
			// Wenn kein Eingabedatenstrom angegeben wurde ...
			if (inputStream == null) 
                // Ausnahme werfen
                throw new ArgumentNullException("inputStream");

            // Wenn kein Kryptografieanbieter angegeben wurde ...
            if (provider == null) 
                // Ausnahme werfen
                throw new ArgumentNullException("provider");

			// Ausgabedatenstrom erzeugen
			MemoryStream outputStream = new MemoryStream();
			
            // Verschlüsselungsdatenstrom erzeugen
            CryptoStream encryptStream = new CryptoStream(outputStream, 
                                                          provider.CreateEncryptor(), 
                                                          CryptoStreamMode.Write);

			// Variable für aktuelle Position
			int position;

            // Eingabepuffer erzeugen
			byte [] inputBuffer = new byte[_bufferSize];
			
            // Solange das Ende des Eingabedatenstroms noch nicht erreicht ist ...
            while((position = inputStream.Read(inputBuffer, 0, inputBuffer.Length)) != 0) 
			{
                // Daten im Eingabepuffer an den Verschlüsselungsdatenstrom schreiben
				encryptStream.Write(inputBuffer, 0, position);
			}
            // Sicherstellen, dass alle Daten verschlüsselt wurden
			encryptStream.FlushFinalBlock();

			// Position des Ausgabedatenstroms auf 0 zurücksetzen
			outputStream.Position = 0;

            // Ausgabedatenstrom zurückgeben
			return outputStream;
		}

        /// <summary>
        /// Entschlüsselt einen bestimmten symmetrisch verschlüsselten Datenstrom gibt den entschlüsselten Datenstrom zurück.
        /// <remarks>
        /// Der entschlüsselte Datenstrom wird automatisch auf Position 0 zurückgesetzt.
        /// </remarks>
        /// </summary>
        /// <param name="inputStream">Verschlüsselter Eingabedatenstrom</param>
        /// <param name="provider">Kryptografieanbieter für symmetrische Verschlüsselung</param>
        /// <returns>Entschlüsselter Datenstrom</returns>
		public static Stream GetDecryptedStream(Stream inputStream, SymmetricAlgorithm provider) 
		{
            // Wenn kein Eingabedatenstrom angegeben wurde ...
            if (inputStream == null)
                // Ausnahme werfen
                throw new ArgumentNullException("inputStream");

            // Wenn kein Kryptografieanbieter angegeben wurde ...
            if (provider == null)
                // Ausnahme werfen
                throw new ArgumentNullException("provider");

            // Entschlüsselungsdatenstrom erzeugen
            CryptoStream decryptStream = new CryptoStream(inputStream,
                                                          provider.CreateDecryptor(),
                                                          CryptoStreamMode.Read);

            // Ausgabedatenstrom erzeugen
            MemoryStream outputStream = new MemoryStream();

            // Variable für aktuelle Position
            int position;

            // Eingabepuffer erzeugen
            byte[] inputBuffer = new byte[_bufferSize];

            // Solange das Ende des Eingabedatenstroms noch nicht erreicht ist ...			
			while((position = decryptStream.Read(inputBuffer, 0, inputBuffer.Length)) != 0) 
			{
                // Entschlüsselte Daten vom Eingabepuffer in den Ausgabedatenstrom schreiben
				outputStream.Write(inputBuffer, 0, position);
			}
            // Ausgabedatenstrom auf Position 0 zurücksetzen
			outputStream.Position = 0;
			
            // Ausgabedatenstrom zurückgeben
            return outputStream;
		}
	}
}

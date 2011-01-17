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
		// Puffergr��e in Byte
		private const int _bufferSize = 2048;	
	
		/// <summary>
        /// Erzeugt einen Kryptografieanbieter f�r symmetrische Verschl�sselung.         ///
        /// <remarks>
        /// Folgende Algorithmen weren unterst�tzt:
        /// 
        /// "DES", "3DES", "RIJNDAEL" und "RC2"
        /// </remarks>
        /// </summary>
		/// <param name="algorithm">Name des zu verwendenden Verschl�sselungsalgorithmus (z.B. "3DES")</param>
        /// <returns>Kryptografieanbieter f�r symmetrische Verschl�sselung</returns>		
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
                    throw new ArgumentException(string.Format("Der angegeben Verschl�sselungsalgorithmus '{0}' wird nicht unterst�tzt. Bitte geben Sie einen der folgende Algorithmen an: '3DES', 'DES', 'RIJNDAEL' oder 'RC2'.",algorithm), "algorithm");
			}
		}
        
		/// <summary>
		/// Verschl�sselt einen bestimmten Datenstrom symmetrisch und gibt den verschl�sselten Datenstrom zur�ck.
        /// <remarks>
        /// Der verschl�sselte Datenstrom wird automatisch auf Position 0 zur�ckgesetzt.
        /// </remarks>
		/// </summary>
		/// <param name="inputStream">Unverschl�sselter Eingabedatenstrom</param>
        /// <param name="provider">Kryptografieanbieter f�r symmetrische Verschl�sselung</param>
		/// <returns>Verschl�sselter Datenstrom</returns>
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
			
            // Verschl�sselungsdatenstrom erzeugen
            CryptoStream encryptStream = new CryptoStream(outputStream, 
                                                          provider.CreateEncryptor(), 
                                                          CryptoStreamMode.Write);

			// Variable f�r aktuelle Position
			int position;

            // Eingabepuffer erzeugen
			byte [] inputBuffer = new byte[_bufferSize];
			
            // Solange das Ende des Eingabedatenstroms noch nicht erreicht ist ...
            while((position = inputStream.Read(inputBuffer, 0, inputBuffer.Length)) != 0) 
			{
                // Daten im Eingabepuffer an den Verschl�sselungsdatenstrom schreiben
				encryptStream.Write(inputBuffer, 0, position);
			}
            // Sicherstellen, dass alle Daten verschl�sselt wurden
			encryptStream.FlushFinalBlock();

			// Position des Ausgabedatenstroms auf 0 zur�cksetzen
			outputStream.Position = 0;

            // Ausgabedatenstrom zur�ckgeben
			return outputStream;
		}

        /// <summary>
        /// Entschl�sselt einen bestimmten symmetrisch verschl�sselten Datenstrom gibt den entschl�sselten Datenstrom zur�ck.
        /// <remarks>
        /// Der entschl�sselte Datenstrom wird automatisch auf Position 0 zur�ckgesetzt.
        /// </remarks>
        /// </summary>
        /// <param name="inputStream">Verschl�sselter Eingabedatenstrom</param>
        /// <param name="provider">Kryptografieanbieter f�r symmetrische Verschl�sselung</param>
        /// <returns>Entschl�sselter Datenstrom</returns>
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

            // Entschl�sselungsdatenstrom erzeugen
            CryptoStream decryptStream = new CryptoStream(inputStream,
                                                          provider.CreateDecryptor(),
                                                          CryptoStreamMode.Read);

            // Ausgabedatenstrom erzeugen
            MemoryStream outputStream = new MemoryStream();

            // Variable f�r aktuelle Position
            int position;

            // Eingabepuffer erzeugen
            byte[] inputBuffer = new byte[_bufferSize];

            // Solange das Ende des Eingabedatenstroms noch nicht erreicht ist ...			
			while((position = decryptStream.Read(inputBuffer, 0, inputBuffer.Length)) != 0) 
			{
                // Entschl�sselte Daten vom Eingabepuffer in den Ausgabedatenstrom schreiben
				outputStream.Write(inputBuffer, 0, position);
			}
            // Ausgabedatenstrom auf Position 0 zur�cksetzen
			outputStream.Position = 0;
			
            // Ausgabedatenstrom zur�ckgeben
            return outputStream;
		}
	}
}

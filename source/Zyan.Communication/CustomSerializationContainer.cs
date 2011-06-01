using System;

namespace Zyan.Communication
{
    /// <summary>
    /// Container für Objekte die mit benutzerdefinierter Serialisierung verarbeitet werden.
    /// </summary>
    [Serializable]
    public class CustomSerializationContainer
    {
        /// <summary>
        /// Erstellt eine neue Instanz der CustomSerializationContainer-Klasse.
        /// </summary>
        public CustomSerializationContainer()
        {

        }
        
        /// <summary>
        /// Erstellt eine neue Instanz der CustomSerializationContainer-Klasse.
        /// </summary>
        /// <param name="handledType">Behandelter Typ</param>
        /// <param name="dataType">Tatsächlicher der Daten</param>
        /// <param name="data">Rohdaten</param>
        public CustomSerializationContainer(Type handledType, Type dataType, byte[] data)
        {
            // Eigenschaften füllen            
            HandledType = handledType;
            DataType = dataType;
            Data = data;
        }

        /// <summary>
        /// Gibt den behandelten Typ zurück, oder legt ih nfest.
        /// </summary>
        public Type HandledType
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt den tatsächlichen Typ der Daten zurück, oder legt ihn fest.
        /// </summary>
        public Type DataType
        {
            get;
            set;
        }
                
        /// <summary>
        /// Gibt die Rohdaten zurück, oder legt sie fest.
        /// </summary>
        public byte[] Data
        {
            get;
            set;
        }
    }
}

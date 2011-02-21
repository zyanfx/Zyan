using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication
{
    /// <summary>
    /// Implementieren Sie diese Schnittstelle, um eine benutzerdefinierte Serialisierung für einen bestimmten Typen zu erstellen.
    /// </summary>
    public interface ISerializationHandler
    {        
        /// <summary>
        /// Serailisiert ein Objekt.
        /// </summary>
        /// <param name="data">Objekt</param>
        /// <returns>Rohdaten</returns>
        byte[] Serialize(object data);

        /// <summary>
        /// Deserialisiert Rohdaten in ein Objekt eines bestimmten Typs.
        /// </summary>
        /// <param name="dataType">Typ der Daten</param>
        /// <param name="data">Rohdaten</param>
        /// <returns>Objekt</returns>
        object Deserialize(Type dataType, byte[] data);
    }
}

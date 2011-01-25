using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication
{
    /// <summary>
    /// Schreibt eine veröffentlichte Komponente.
    /// </summary>
    [Serializable]
    public class ComponentInfo
    {
        /// <summary>
        /// Gibt den Schnittstellennamen zurück, oder legt ihn fest.
        /// </summary>
        public string InterfaceName
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt den Aktivierungstyp zurück, oder legt ihn fest.
        /// </summary>
        public ActivationType ActivationType
        {
            get;
            set;
        }
    }
}

using System;

namespace Zyan.Communication.Protocols
{
    /// <summary>
    /// Defines all supported kinds of versioning.
    /// </summary>
    public enum Versioning : short
    { 
        /// <summary>
        /// Strict versioning. Version numbers of strong name assemblies are checked.
        /// </summary>
        Strict=1,
        /// <summary>
        /// Version numbers of assembly (even strong named) are ignored.
        /// </summary>
        Lax
    }
}
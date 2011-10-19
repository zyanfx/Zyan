using System;

namespace Util
{
    /// <summary>
    /// Marks the class as an exempt from compression.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NonCompressible : Attribute
    {
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Modularity
{   
    /// <summary>
    /// Markiert eine Klasse als Modul-Kontroller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false,AllowMultiple=true)]
    public sealed class ZyanModuleControlerAttribute : Attribute
    {
    }
}

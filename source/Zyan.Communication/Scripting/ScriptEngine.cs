using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace Zyan.Communication.Scripting
{
    /// <summary>
    /// Kompiliert dynamisch C#-Quellcode zur Luafzeit.
    /// </summary>
    public static class ScriptEngine
    {
        /// <summary>
        /// Erzeugt aus C#-Quellcode eine ausführbare Assembly im Speicher.
        /// </summary>
        /// <param name="scriptCode">C#-Quellcode</param>
        /// <returns>Kompilierte Assembly</returns>
        public static Assembly CompileScriptToAssembly(string scriptCode, params string[] referenceAsseblies)
        {
            // Parameter für den Kompilierung festlegen
            CompilerParameters cp = new CompilerParameters()
            {
                GenerateExecutable=false,
                GenerateInMemory=true,                
            };            
            // Wenn zusätzliche Verweise angegeben wurden ...
            if (referenceAsseblies.Length > 0)
                // Zusätzliche Verweise anfügen
                cp.ReferencedAssemblies.AddRange(referenceAsseblies);
            else
                // Standardverweise anfügen
                cp.ReferencedAssemblies.AddRange(new string[] 
                {
                    "System.dll",
                    "System.Core.dll"
                });

            // C#-Compiler erzeugen
            using (CSharpCodeProvider codeProvider = new CSharpCodeProvider())
            {
                // C#-Skript kompilieren
                CompilerResults results = codeProvider.CompileAssemblyFromSource(cp, scriptCode);

                // Wenn keine Fehler aufgetreten sind ...
                if (results.Errors.Count == 0)
                    // Assembly zurückgeben
                    return results.CompiledAssembly;
            }
            // Nichts zurückgeben
            return null;
        }
    }
}

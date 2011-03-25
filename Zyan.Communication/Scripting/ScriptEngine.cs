using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.CodeDom;

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
        /// <param name="referenceAsseblies">Array mit referenzierten Assemblynamen</param>
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
            {
                // Zusätzliche Verweise anfügen
                cp.ReferencedAssemblies.AddRange(referenceAsseblies);

                if ((from referenceAssembly in referenceAsseblies where referenceAssembly.Equals("System.Xml.dll",StringComparison.InvariantCultureIgnoreCase) select referenceAssembly).Count()==0)
                    cp.ReferencedAssemblies.Add("System.Xml.dll");
            }
            else
                // Standardverweise anfügen
                cp.ReferencedAssemblies.AddRange(new string[] 
                {
                    "System.dll",
                    "System.Core.dll",
                    "System.Xml.dll"
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

        /// <summary>
        /// Gibt den C#-Namen eines bestimmten Typs zurück.
        /// </summary>
        /// <param name="type">Typ</param>
        /// <returns>C# Name des Typs</returns>
        public static string GetCSharpNameOfType(Type type)
        {
            // Wenn kein Typ angegeben wurde ...
            if (type == null)
                // Ausnahme werden
                throw new ArgumentNullException("type");

            // Wenn der Typ kein generischer Typ ist ...
            if (!type.IsGenericType)
                // Vollständigen Namen zurückgeben
                return type.FullName.Replace('+','.');

            // Quellcode-Typverweis erzeugen
            CodeTypeReference typeRef = new CodeTypeReference(type);

            // C#-Quellcodeanbieter erzeugen
            CSharpCodeProvider provider = new CSharpCodeProvider();

            // Quellcodename des Typs ermitteln und zurückgeben
            return provider.GetTypeOutput(typeRef).Replace('+','.');
        }
    }
}

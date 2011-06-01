using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;

namespace Zyan.Communication.Scripting
{
    /// <summary>
    /// Compiles C# source code on runtime.
    /// </summary>
    public static class ScriptEngine
    {
        /// <summary>
        /// Creates a compiled assembly from source code.
        /// </summary>
        /// <param name="scriptCode">C# source code</param>
        /// <param name="referenceAsseblies">Array with assembly file paths to reference</param>
        /// <returns>Compiled assembly</returns>
        public static Assembly CompileScriptToAssembly(string scriptCode, params string[] referenceAsseblies)
        {
            CompilerParameters cp = new CompilerParameters()
            {
                GenerateExecutable=false,
                GenerateInMemory=true,                
            };            
            if (referenceAsseblies.Length > 0)
                cp.ReferencedAssemblies.AddRange(referenceAsseblies);
            else
                cp.ReferencedAssemblies.AddRange(new string[] 
                {
                    "System.dll",
                    "System.Core.dll",
                    "System.Xml.dll",
                    "System.Data.dll",
                    "System.Data.DataSetExtensions.dll"
                });
            
            using (CSharpCodeProvider codeProvider = new CSharpCodeProvider())
            {
                CompilerResults results = codeProvider.CompileAssemblyFromSource(cp, scriptCode);

                if (results.Errors.Count == 0)
                    return results.CompiledAssembly;
            }
            return null;
        }

        /// <summary>
        /// Gets the C# name of a specified type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>C# name</returns>
        public static string GetCSharpNameOfType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (!type.IsGenericType)
                return type.FullName.Replace('+','.');

            CodeTypeReference typeRef = new CodeTypeReference(type);
            CSharpCodeProvider provider = new CSharpCodeProvider();

            return provider.GetTypeOutput(typeRef).Replace('+','.');
        }
    }
}

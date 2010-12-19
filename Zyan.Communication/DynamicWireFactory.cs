using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Zyan.Communication.Scripting;

namespace Zyan.Communication
{
    /// <summary>
    /// Fabrik für dynamische Drähte.
    /// </summary>
    internal sealed class DynamicWireFactory
    {
        // Sperrobjekt für Threadsynchronisierung bei Zugriff auf die Singlton-Instanz
        private static object _singltonLockObject = new object();

        // Singleton-Instanz
        private static volatile DynamicWireFactory _singleton=null;

        // Wörterbuch zur Zwischenspeicherung von dynamischen Drahttypen
        private Dictionary<string, Type> _wireTypeCache = null;

        /// <summary>
        /// Gibt die Singleton-Instanz der fabrik zurück.
        /// </summary>
        public static DynamicWireFactory Instance
        {
            get 
            {
                // Wenn noch keine Singlton-Instanz existiert ...
                if (_singleton == null)
                {
                    lock (_singltonLockObject)
                    {
                        // Wenn nicht zwischenzeitlich eine Singleton-Instanz durch einen anderen Thread erstellt wurde ...
                        if (_singleton == null)
                            // Singleton-Instanz erstellen
                            _singleton = new DynamicWireFactory();
                    }
                }
                // Singleton-Instanz zurückgeben
                return _singleton;
            }
        }

        /// <summary>
        ///  Erzeugt eine neue Instanz von DynamicWireFactory.
        /// </summary>
        private DynamicWireFactory()
        {
            // Drahttyp-Cache erzeugen
            _wireTypeCache = new Dictionary<string, Type>();
        }

        // Objekt zur Threadsynchnonisierung beim Zugriff auf den Drahtypen-Zwischenspeicher
        private object _wireTypeCacheLockObject = new object();

        /// <summary>
        /// Erzeugt einen dynamischen Draht für einen bestimmten Ausgangs-Pin einer Komponente. 
        /// </summary>
        /// <param name="componentType">Typ der Komponente</param>
        /// <param name="outPinPropertyName">Eigenschaftsname des Ausgangs-Pins</param>
        /// <returns>Instanz des passenden dynamischen Drahts</returns>
        public object CreateDynamicWire(Type componentType, string outPinPropertyName)
        {
            // Wenn kein Komponententyp angegeben wurde ...
            if (componentType == null)
                // Ausnahme werfen
                throw new ArgumentNullException("componentType");

            // Wenn kein Ausgangs-Pin-Name angegeben wurde ...
            if (string.IsNullOrEmpty(outPinPropertyName))
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_OutPutPinNameMissing, "outPinPropertyName");

            // Schlüssel aus Komponententyp und Ausgangs-Pin-Namen erzeugen
            StringBuilder wireKeyBuilder = new StringBuilder();
            wireKeyBuilder.Append(componentType.FullName);
            wireKeyBuilder.Append("|");
            wireKeyBuilder.Append(outPinPropertyName);
            string wireKey = wireKeyBuilder.ToString();

            // Variable für Drahttyp
            Type wireType=null;

            // Schalter, der angibt, ob bereits ein passender Drahttyp vorhanden ist
            bool wireTypeAlreadyCreated=false;

            lock (_wireTypeCacheLockObject)
            {
                // Prüfen ob bereits ein passender dynamischer Draht für den Ausgangs-Pin existiert ...
                wireTypeAlreadyCreated=_wireTypeCache.ContainsKey(wireKey);
            }
            // Wenn bereits ein passender dynamischer Draht für diesen Ausgangs-Pin ezeugt wurde ...
            if (wireTypeAlreadyCreated)
                // Bestehenden Drahttyp verwenden
                wireType = _wireTypeCache[wireKey];
            else
            {
                
                // Passenden Drahtyp erzeugen
                wireType = BuildDynamicWireType(componentType, outPinPropertyName);

                lock (_wireTypeCacheLockObject)
                {
                    // Wenn in der Zwischenzeit ein passender Drahttyp durch einen anderen Thread erzeugt wurde ...
                    if (_wireTypeCache.ContainsKey(wireKey))
                        // Bestehenden Drahttyp verwenden
                        wireType = _wireTypeCache[wireKey];
                    else
                        // Drahttyp zwischenspeichern
                        _wireTypeCache.Add(wireKey, wireType);
                }
            }
            // Drahtinstanz erzeugen 
            object wire = Activator.CreateInstance(wireType);

            // Drahtinstanz zurückgeben
            return wire;
        }

        /// <summary>
        /// Erzeugt einen dynamischen Draht für einen bestimmten Ausgangs-Pin einer Komponente.
        /// </summary>
        /// <param name="componentType">Typ der Komponente</param>
        /// <param name="outPinPropertyName">Eigenschaftsname des Ausgangs-Pins</param>
        /// <returns>Typ des dynamischen Drahts</returns>
        private Type BuildDynamicWireType(Type componentType,string outPinPropertyName)
        {
            // Verweise der Komponenten-Assembly übernehmen
            string[] references = (from assy in componentType.Assembly.GetReferencedAssemblies()
                                   select Assembly.Load(assy).Location).ToArray();
            
            // Metadaten der Ausgangs-Pin-Eigenschaft abrufen
            PropertyInfo outPinInfo = componentType.GetProperty(outPinPropertyName);

            // Quellcode für dynamischen Draht erzeugen
            string sourceCode = CreateDynamicWireSourceCode(outPinInfo);

            // Dynamischen Draht kompilieren
            Assembly assembly = ScriptEngine.CompileScriptToAssembly(sourceCode, references);

            // Typinformationen des dynamischen Drahtes abrufen
            Type dynamicWireType = assembly.GetType("Zyan.Communication.DynamicWire");

            // Typ des dynamischen Drahtes zurückgeben
            return dynamicWireType;
        }

        /// <summary>
        /// Erzeugt den Quellcode für einen dynamischen Draht, der zu einem bestimmten Ausgangs-Pin passt.
        /// </summary>
        /// <param name="outPinInfo">Metadaten der Eigenschaft des Ausgangs-Pins</param>
        /// <returns>Quellcode (C#)</returns>
        private string CreateDynamicWireSourceCode(PropertyInfo outPinInfo)
        {   
            // Delegat-Typ des Ausgangs-Pins ermitteln
            Type outPinDelegateType = outPinInfo.PropertyType;

            // Metadaten des Delegaten abrufen
            MethodInfo outPinMethod = outPinDelegateType.GetMethod("Invoke");

            // Quellcode für dynamischen Draht zusammensetzen
            StringBuilder code = new StringBuilder();
            code.AppendLine("namespace Zyan.Communication");
            code.AppendLine("{");
            code.AppendLine("   public class DynamicWire");
            code.AppendLine("   {");
            code.AppendLine("       private RemoteOutputPinWiring _clientPinWiring = null;");
            code.AppendLine("       public RemoteOutputPinWiring ClientPinWiring { get {return _clientPinWiring;} set { _clientPinWiring = value; } }");

            // Ermitteln, ob der Delegat einen Rückgabewert bescheibt
            bool hasReturnValue = !outPinMethod.ReturnType.Name.Equals("Void");

            // Wenn der Delegat keinen Rückgabewert bescheibt ...
            if (!hasReturnValue)
                // void verwenden
                code.Append("       public void In(");
            else
                // Rückgabetyp verwenden
                code.AppendFormat("       public {0} In(", outPinMethod.ReturnType.FullName);

            // Variable für Anzahl der Parameter, die der Delegat bescheibt
            int argCount = 0;

            // Parameter des Delegaten abrufen
            ParameterInfo[] argInfos = outPinMethod.GetParameters();

            // Alle Parameter durchlaufen
            foreach (ParameterInfo argInfo in argInfos)
            {
                // Parameterzähler erhöhen
                argCount++;

                // Parameterdefinition in Quellcode schreiben
                code.AppendFormat("{0} {1}", argInfo.ParameterType.FullName, argInfo.Name);

                // Wenn es nicht der letzte Parameter ist ...
                if (argCount < argInfos.Length)
                    // Komma in Quellcode schreiben
                    code.Append(", ");
            }
            // Weiteren Quellcode schreiben
            code.Append(")");
            code.AppendLine();
            code.AppendLine("       {");
            code.Append("           ");

            // Wenn der Delegat keinen Rückgabewert beschreibt ...
            if (!hasReturnValue)
                // InvokeDynamicClientPin aufrufen
                code.Append("ClientPinWiring.InvokeDynamicClientPin(");
            else
                // InvokeDynamicClientPin aufrufen und Rückgabewert zurückgeben (Typenumwandlung durchführen)
                code.AppendFormat("return ({0})ClientPinWiring.InvokeDynamicClientPin(", outPinMethod.ReturnType.FullName);

            // Parameterzähler zurücksetzen
            argCount = 0;

            // Alle Parameter des Delegaten durchlaufen
            foreach (ParameterInfo argInfo in argInfos)
            {
                // Parameterzähler erhöhen
                argCount++;

                // Paramtername an Quellcode anfügen
                code.Append(argInfo.Name);

                // Wenn es nicht der letzte Parameter ist ...
                if (argCount < argInfos.Length)
                    // Komma an Quellcode anfügen
                    code.Append(", ");
            }
            // Restlichen Quellcode schreiben
            code.AppendLine(");");
            code.AppendLine("       }");
            code.AppendLine("   }");
            code.AppendLine("}");

            // Quellcode zurückgeben
            return code.ToString();
        }
    }
}

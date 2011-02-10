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
        /// Erzeugt einen dynamischen Draht für ein bestimmtes Ereignis einer Komponente. 
        /// </summary>
        /// <param name="componentType">Typ der Komponente</param>
        /// <param name="eventMemberName">Name des Ereignisses oder der Delegat-Eigenschaft</param>
        /// <param name="isEvent">Gibt an, ob der Draht als Ereignis implementiert ist, oder nicht</param>
        /// <returns>Instanz des passenden dynamischen Drahts</returns>
        public object CreateDynamicWire(Type componentType, string eventMemberName, bool isEvent)
        {
            // Wenn kein Komponententyp angegeben wurde ...
            if (componentType == null)
                // Ausnahme werfen
                throw new ArgumentNullException("componentType");

            // Wenn kein Ereignisname angegeben wurde ...
            if (string.IsNullOrEmpty(eventMemberName))
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_OutPutPinNameMissing, "eventMemberName");

            // Schlüssel aus Komponententyp und Ereignisnamen erzeugen
            StringBuilder wireKeyBuilder = new StringBuilder();
            wireKeyBuilder.Append(componentType.FullName);
            wireKeyBuilder.Append("|");
            wireKeyBuilder.Append(eventMemberName);
            string wireKey = wireKeyBuilder.ToString();

            // Variable für Drahttyp
            Type wireType=null;

            // Schalter, der angibt, ob bereits ein passender Drahttyp vorhanden ist
            bool wireTypeAlreadyCreated=false;

            lock (_wireTypeCacheLockObject)
            {
                // Prüfen ob bereits ein passender dynamischer Draht für das Ereignis existiert ...
                wireTypeAlreadyCreated=_wireTypeCache.ContainsKey(wireKey);
            }
            // Wenn bereits ein passender dynamischer Draht für dieses Ereignis ezeugt wurde ...
            if (wireTypeAlreadyCreated)
                // Bestehenden Drahttyp verwenden
                wireType = _wireTypeCache[wireKey];
            else
            {                
                // Passenden Drahtyp erzeugen
                wireType = BuildDynamicWireType(componentType, eventMemberName, isEvent);

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
        /// Erzeugt einen dynamischen Draht für ein bestimmtes Ereignis einer Komponente. 
        /// </summary>
        /// <param name="componentType">Typ der Komponente</param>
        /// <param name="delegateType">Delegattyp</param>
        /// <param name="clientInterceptor">Client-Abfangvorrichtung</param>
        /// <returns>Instanz des passenden dynamischen Drahts</returns>
        public object CreateDynamicWire(Type componentType, Type delegateType, DelegateInterceptor clientInterceptor)
        {
            // Wenn kein Komponententyp angegeben wurde ...
            if (componentType == null)
                // Ausnahme werfen
                throw new ArgumentNullException("componentType");

            // Wenn kein Delegattyp angegeben wurde ...
            if (delegateType == null)
                // Ausnahme werfen
                throw new ArgumentNullException("delegateType");

            // Wenn keine Client-Abfangeinrichtung angegeben wurde ...
            if (clientInterceptor == null)
                // Ausnahme werfen
                throw new ArgumentNullException("clientInterceptor");
                        
            // Passenden Drahtyp erzeugen
            Type wireType = BuildDynamicWireType(componentType, delegateType, clientInterceptor);
                                    
            // Drahtinstanz erzeugen 
            object wire = Activator.CreateInstance(wireType);

            // Drahtinstanz zurückgeben
            return wire;
        }

        /// <summary>
        /// Erzeugt den Typen für einen dynamischen Draht für ein bestimmtes Ereignis einer Komponente.
        /// </summary>
        /// <param name="componentType">Typ der Komponente</param>
        /// <param name="delegateType">Delegattyp</param>
        /// <param name="clientInterceptor">Client-Abfangvorrichtung</param>
        /// <returns>Typ des dynamischen Drahts</returns>
        private Type BuildDynamicWireType(Type componentType, Type delegateType, DelegateInterceptor clientInterceptor)
        {
            // Verweise der Komponenten-Assembly übernehmen
            string[] references = (from assy in componentType.Assembly.GetReferencedAssemblies()
                                   select Assembly.Load(assy).Location).ToArray();

            // Variable für Quellcode
            string sourceCode = string.Empty;
            
            // Quellcode für dynamischen Draht erzeugen
            sourceCode = CreateDynamicWireSourceCodeForDelegate(delegateType);
            
            // Dynamischen Draht kompilieren
            Assembly assembly = ScriptEngine.CompileScriptToAssembly(sourceCode, references);

            // Typinformationen des dynamischen Drahtes abrufen
            Type dynamicWireType = assembly.GetType("Zyan.Communication.DynamicWire");

            // Typ des dynamischen Drahtes zurückgeben
            return dynamicWireType;
        }

        /// <summary>
        /// Erzeugt den Typen für einen dynamischen Draht für ein bestimmtes Ereignis einer Komponente.
        /// </summary>
        /// <param name="componentType">Typ der Komponente</param>
        /// <param name="eventMemberName">Name des Ereignisses oder der Delegat-Eigenschaft</param>
        /// <param name="isEvent">Gibt an, ob der Draht als Ereignis implementiert ist, oder nicht</param>
        /// <returns>Typ des dynamischen Drahts</returns>
        private Type BuildDynamicWireType(Type componentType,string eventMemberName, bool isEvent)
        {
            // Verweise der Komponenten-Assembly übernehmen
            string[] references = (from assy in componentType.Assembly.GetReferencedAssemblies()
                                   select Assembly.Load(assy).Location).ToArray();
            
            // Variable für Quellcode
            string sourceCode = string.Empty;

            // Wenn der Draht als Ereignis implementiert ist ...
            if (isEvent)
            {
                // Metadaten des Ereignisses abrufen
                EventInfo eventInfo = componentType.GetEvent(eventMemberName);

                // Quellcode für dynamischen Draht erzeugen
                sourceCode = CreateDynamicWireSourceCodeForEvent(eventInfo);
            }
            else
            {
                // Metadaten der Delegat-Eigenschaft abrufen
                PropertyInfo delegatePropInfo = componentType.GetProperty(eventMemberName);

                // Quellcode für dynamischen Draht erzeugen
                sourceCode = CreateDynamicWireSourceCodeForDelegate(delegatePropInfo);
            }
            // Dynamischen Draht kompilieren
            Assembly assembly = ScriptEngine.CompileScriptToAssembly(sourceCode, references);

            // Typinformationen des dynamischen Drahtes abrufen
            Type dynamicWireType = assembly.GetType("Zyan.Communication.DynamicWire");

            // Typ des dynamischen Drahtes zurückgeben
            return dynamicWireType;
        }

        /// <summary>
        /// Erzeugt den Quellcode für einen dynamischen Draht, der zu einem bestimmten Ereignis passt.
        /// </summary>
        /// <param name="eventInfo">Metadaten des Ereignisses</param>
        /// <returns>Quellcode (C#)</returns>
        private string CreateDynamicWireSourceCodeForEvent(EventInfo eventInfo)
        {
            // Delegat-Typ des Ereignisses ermitteln
            Type eventType = eventInfo.EventHandlerType;

            // Metadaten des Delegaten abrufen
            MethodInfo eventMethod = eventType.GetMethod("Invoke");

            // Quellcode für dynamischen Draht zusammensetzen
            StringBuilder code = new StringBuilder();
            code.AppendLine("namespace Zyan.Communication");
            code.AppendLine("{");
            code.AppendLine("   using System;");
            code.AppendLine("   using System.Reflection;");
            code.AppendLine("   public class DynamicWire");
            code.AppendLine("   {");
            code.AppendLine("       private object _component = null;");
            code.AppendLine("       public object Component { get { return _component; } set { _component = value; } }");
            code.AppendLine("       private System.Reflection.EventInfo _serverEventInfo = null;");
            code.AppendLine("       public System.Reflection.EventInfo ServerEventInfo { get { return _serverEventInfo; } set { _serverEventInfo = value; } }");
            code.AppendLine("       private DelegateInterceptor _interceptor = null;");
            code.AppendLine("       public DelegateInterceptor Interceptor { get { return _interceptor; } set { _interceptor = value; } }");
            
            // Ermitteln, ob der Delegat einen Rückgabewert bescheibt
            bool hasReturnValue = !eventMethod.ReturnType.Name.Equals("Void");

            // Wenn der Delegat keinen Rückgabewert bescheibt ...
            if (!hasReturnValue)
                // void verwenden
                code.Append("       public void In(");
            else
                // Rückgabetyp verwenden
                code.AppendFormat("       public {0} In(", eventMethod.ReturnType.FullName);

            // Variable für Anzahl der Parameter, die der Delegat bescheibt
            int argCount = 0;

            // Parameter des Delegaten abrufen
            ParameterInfo[] argInfos = eventMethod.GetParameters();

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
                        
            // Wenn der Delegat keinen Rückgabewert beschreibt ...
            if (!hasReturnValue)
                // InvokeClientDelegate aufrufen
                code.Append("try { Interceptor.InvokeClientDelegate(");
            else
                // InvokeClientDelegate aufrufen und Rückgabewert zurückgeben (Typenumwandlung durchführen)
                code.AppendFormat("try { return ({0})Interceptor.InvokeClientDelegate(", eventMethod.ReturnType.FullName);

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
            code.Append("); } catch (Exception ex) {");
            code.AppendLine("           Type dynamicWireType = this.GetType();");
            code.AppendLine("           Delegate dynamicWireDelegate = Delegate.CreateDelegate(ServerEventInfo.EventHandlerType, this, dynamicWireType.GetMethod(\"In\"));");
            code.AppendLine("           ServerEventInfo.RemoveEventHandler(Component, dynamicWireDelegate);");
            code.AppendLine("           throw ex; }");
            code.AppendLine("       }");
            code.AppendLine("   }");
            code.AppendLine("}");

            // Quellcode zurückgeben
            return code.ToString();
        }

        /// <summary>
        /// Erzeugt den Quellcode für einen dynamischen Draht, der zu einem bestimmten Delegat passt.
        /// </summary>
        /// <param name="delegatePropInfo">Metadaten der Delegat-Eigenschaft</param>
        /// <returns>Quellcode (C#)</returns>
        private string CreateDynamicWireSourceCodeForDelegate(PropertyInfo delegatePropInfo)
        {
            // Delegat-Typ ermitteln
            Type delegateType = delegatePropInfo.PropertyType;

            // Andere Überladung aufrufen
            return CreateDynamicWireSourceCodeForDelegate(delegateType);
        }

        /// <summary>
        /// Erzeugt den Quellcode für einen dynamischen Draht, der zu einem bestimmten Delegat passt.
        /// </summary>
        /// <param name="delegateType">Delegattyp</param>
        /// <returns>Quellcode (C#)</returns>
        private string CreateDynamicWireSourceCodeForDelegate(Type delegateType)
        {
            // Metadaten des Delegaten abrufen
            MethodInfo delegateMethod = delegateType.GetMethod("Invoke");

            // Quellcode für dynamischen Draht zusammensetzen
            StringBuilder code = new StringBuilder();
            code.AppendLine("namespace Zyan.Communication");
            code.AppendLine("{");
            code.AppendLine("   public class DynamicWire");
            code.AppendLine("   {");
            code.AppendLine("       private DelegateInterceptor _interceptor = null;");
            code.AppendLine("       public DelegateInterceptor Interceptor { get {return _interceptor;} set { _interceptor = value; } }");

            // Ermitteln, ob der Delegat einen Rückgabewert bescheibt
            bool hasReturnValue = !delegateMethod.ReturnType.Name.Equals("Void");

            // Wenn der Delegat keinen Rückgabewert bescheibt ...
            if (!hasReturnValue)
                // void verwenden
                code.Append("       public void In(");
            else
                // Rückgabetyp verwenden
                code.AppendFormat("       public {0} In(", delegateMethod.ReturnType.FullName);

            // Variable für Anzahl der Parameter, die der Delegat bescheibt
            int argCount = 0;

            // Parameter des Delegaten abrufen
            ParameterInfo[] argInfos = delegateMethod.GetParameters();

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
                // InvokeClientDelegate aufrufen
                code.Append("Interceptor.InvokeClientDelegate(");
            else
                // InvokeClientDelegate aufrufen und Rückgabewert zurückgeben (Typenumwandlung durchführen)
                code.AppendFormat("return ({0})Interceptor.InvokeClientDelegate(", delegateMethod.ReturnType.FullName);

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

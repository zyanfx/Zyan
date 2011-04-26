using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Zyan.Communication.Scripting;
using System.IO;

namespace Zyan.Communication
{
    /// <summary>
    /// Factory class for creation of dynamic wires.    
    /// </summary>
    internal sealed class DynamicWireFactory
    {
        // Locking object
        private static object _singltonLockObject = new object();

        // Singleton instance
        private static volatile DynamicWireFactory _singleton=null;

        // Cache for created dynamic wire types (creation is very expensive, so the types are cached)
        private Dictionary<string, Type> _wireTypeCache = null;

        /// <summary>
        /// Gets a singleton instance of the DynamicWirefactory class.
        /// </summary>
        public static DynamicWireFactory Instance
        {
            get 
            {
                if (_singleton == null)
                {
                    lock (_singltonLockObject)
                    {
                        if (_singleton == null)
                            _singleton = new DynamicWireFactory();
                    }
                }
                return _singleton;
            }
        }

        /// <summary>
        /// Creates a new instance of the DynamicWireFactory class.
        /// </summary>
        private DynamicWireFactory()
        {
            _wireTypeCache = new Dictionary<string, Type>();
        }

        // Locking object for thread sync of the wire type cache
        private object _wireTypeCacheLockObject = new object();

        /// <summary>
        /// Creates a dynamic wire for a specified event or delegate property of a component.        
        /// </summary>
        /// <param name="componentType">Component type</param>
        /// <param name="eventMemberName">Event name or name of the delegate property</param>
        /// <param name="isEvent">Sets if the member is a event (if false, the memeber must be a delegate property)</param>
        /// <returns>Instance of the created dynamic wire type (ready to use)</returns>
        public object CreateDynamicWire(Type componentType, string eventMemberName, bool isEvent)
        {
            if (componentType == null)
                throw new ArgumentNullException("componentType");

            if (string.IsNullOrEmpty(eventMemberName))
                throw new ArgumentException(LanguageResource.ArgumentException_OutPutPinNameMissing, "eventMemberName");

            StringBuilder wireKeyBuilder = new StringBuilder();
            wireKeyBuilder.Append(componentType.FullName);
            wireKeyBuilder.Append("|");
            wireKeyBuilder.Append(eventMemberName);
            string wireKey = wireKeyBuilder.ToString();

            Type wireType=null;

            bool wireTypeAlreadyCreated=false;

            lock (_wireTypeCacheLockObject)
            {
                wireTypeAlreadyCreated=_wireTypeCache.ContainsKey(wireKey);
            }
            if (wireTypeAlreadyCreated)
                wireType = _wireTypeCache[wireKey];
            else
            {                
                wireType = BuildDynamicWireType(componentType, eventMemberName, isEvent);

                lock (_wireTypeCacheLockObject)
                {
                    if (_wireTypeCache.ContainsKey(wireKey))
                        wireType = _wireTypeCache[wireKey];
                    else
                        _wireTypeCache.Add(wireKey, wireType);
                }
            }
            object wire = Activator.CreateInstance(wireType);
            return wire;
        }

        /// <summary>
        /// Creates a dynamic wire for a specified event or delegate property of a component.        
        /// </summary>
        /// <param name="componentType">Component type</param>
        /// <param name="delegateType">Type of the delegate</param>
        /// <param name="clientInterceptor">Interceptor object form client</param>
        /// <returns>Instance of the created dynamic wire type (ready to use)</returns>
        public object CreateDynamicWire(Type componentType, Type delegateType, DelegateInterceptor clientInterceptor)
        {
            if (componentType == null)
                throw new ArgumentNullException("componentType");

            if (delegateType == null)
                throw new ArgumentNullException("delegateType");

            if (clientInterceptor == null)
                throw new ArgumentNullException("clientInterceptor");
                        
            Type wireType = BuildDynamicWireType(componentType, delegateType, clientInterceptor);
                                    
            object wire = Activator.CreateInstance(wireType);
            return wire;
        }

        /// <summary>
        /// Returns all direct and indirect references of a specified component type. 
        /// </summary>
        /// <param name="componentType">Component type</param>
        /// <returns>Array with full paths to the referenced assemblies</returns>
        private string[] GetComponentReferences(Type componentType)
        {
            if (componentType == null)
                throw new ArgumentNullException("componentType");

            List<string> componentReferences = new List<string>();
            FindAssemblyReferences(Assembly.GetExecutingAssembly(), componentReferences);
            FindAssemblyReferences(componentType.Assembly, componentReferences);

            return componentReferences.ToArray();
        }

        /// <summary>
        /// Adds a path to the path list, if the list doesn´t contain the path already. 
        /// </summary>
        /// <param name="path">Path to add</param>
        /// <param name="paths">path list</param>
        /// <returns>Returns true if added and false, if the list contains this path aleady</returns>
        private bool AddReferencePathToList(string path, List<string> paths)
        {
            int found = (from p in paths
                         where p.Equals(path, StringComparison.InvariantCultureIgnoreCase)
                         select p).Count();

            if (found == 0)
            {
                paths.Add(path);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Obtains all direct and indirect references of a specified Assembly.
        /// </summary>
        /// <param name="assembly">Assembly to scan for references</param>
        /// <param name="paths">List of file paths</param>
        private void FindAssemblyReferences(Assembly assembly, List<string> paths)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (paths == null)
                throw new ArgumentNullException("paths");

            AddReferencePathToList(assembly.Location, paths);
                        
            foreach (AssemblyName refName in assembly.GetReferencedAssemblies())
            {
                Assembly reference = Assembly.Load(refName);
                if (AddReferencePathToList(reference.Location, paths))
                    FindAssemblyReferences(reference, paths);
            }            
        }

        /// <summary>
        /// Creates a dynamic wire type dynamicly.
        /// </summary>
        /// <param name="componentType">Type of server component</param>
        /// <param name="delegateType">Delegate type of the wire</param>
        /// <param name="clientInterceptor">Interceptor object from client</param>
        /// <returns>Type of dynamic wire</returns>
        private Type BuildDynamicWireType(Type componentType, Type delegateType, DelegateInterceptor clientInterceptor)
        {            
            string[] references = GetComponentReferences(componentType);
            string sourceCode = string.Empty;
            sourceCode = CreateDynamicWireSourceCodeForDelegate(delegateType);
            
            Assembly assembly = ScriptEngine.CompileScriptToAssembly(sourceCode, references);

            Type dynamicWireType = assembly.GetType("Zyan.Communication.DynamicWire");
            return dynamicWireType;
        }

        /// <summary>
        /// Creates a dynamic wire type dynamicly.
        /// </summary>
        /// <param name="componentType">Type of server component</param>
        /// <param name="eventMemberName">Event name or name of the delegate property</param>
        /// <param name="isEvent">Sets if the member is a event (if false, the memeber must be a delegate property)</param>
        /// <returns>Type of dynamic wire</returns>
        private Type BuildDynamicWireType(Type componentType,string eventMemberName, bool isEvent)
        {
            string[] references = GetComponentReferences(componentType);
            string sourceCode = string.Empty;

            if (isEvent)
            {
                EventInfo eventInfo = componentType.GetEvent(eventMemberName);
                sourceCode = CreateDynamicWireSourceCodeForEvent(eventInfo);
            }
            else
            {
                PropertyInfo delegatePropInfo = componentType.GetProperty(eventMemberName);
                sourceCode = CreateDynamicWireSourceCodeForDelegate(delegatePropInfo);
            }
            Assembly assembly = ScriptEngine.CompileScriptToAssembly(sourceCode, references);
            
            Type dynamicWireType = assembly.GetType("Zyan.Communication.DynamicWire");
            return dynamicWireType;
        }

        /// <summary>
        /// Generates source code for dynamic wiring of a specified server component event.
        /// </summary>
        /// <param name="eventInfo">Event member metadata</param>
        /// <returns>Generated C# source code</returns>
        private string CreateDynamicWireSourceCodeForEvent(EventInfo eventInfo)
        {
            Type eventType = eventInfo.EventHandlerType;
            MethodInfo eventMethod = eventType.GetMethod("Invoke");

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
            
            bool hasReturnValue = !eventMethod.ReturnType.Name.Equals("Void");

            if (!hasReturnValue)
                code.Append("       public void In(");
            else
                code.AppendFormat("       public {0} In(", ScriptEngine.GetCSharpNameOfType(eventMethod.ReturnType));

            int argCount = 0;
            ParameterInfo[] argInfos = eventMethod.GetParameters();

            foreach (ParameterInfo argInfo in argInfos)
            {
                argCount++;

                code.AppendFormat("{0} {1}", ScriptEngine.GetCSharpNameOfType(argInfo.ParameterType), argInfo.Name);

                if (argCount < argInfos.Length)
                    code.Append(", ");
            }
            code.Append(")");
            code.AppendLine();
            code.AppendLine("       {");
                        
            if (!hasReturnValue)
                code.Append("try { Interceptor.InvokeClientDelegate(");
            else
                code.AppendFormat("try { return ({0})Interceptor.InvokeClientDelegate(", ScriptEngine.GetCSharpNameOfType(eventMethod.ReturnType));

            argCount = 0;

            foreach (ParameterInfo argInfo in argInfos)
            {
                argCount++;
                code.Append(argInfo.Name);

                if (argCount < argInfos.Length)
                    code.Append(", ");
            }
            code.Append("); } catch (Exception ex) {");
            code.AppendLine("           Type dynamicWireType = this.GetType();");
            code.AppendLine("           Delegate dynamicWireDelegate = Delegate.CreateDelegate(ServerEventInfo.EventHandlerType, this, dynamicWireType.GetMethod(\"In\"));");
            code.AppendLine("           ServerEventInfo.RemoveEventHandler(Component, dynamicWireDelegate);");
            code.AppendLine("           throw ex; }");
            code.AppendLine("       }");
            code.AppendLine("   }");
            code.AppendLine("}");

            return code.ToString();
        }

        /// <summary>
        /// Generates source code for dynamic wiring of a specified server component delegate property.
        /// </summary>
        /// <param name="delegatePropInfo">Metadata of the delegate proerty</param>
        /// <returns>Generated C# source code</returns>
        private string CreateDynamicWireSourceCodeForDelegate(PropertyInfo delegatePropInfo)
        {
            Type delegateType = delegatePropInfo.PropertyType;
            return CreateDynamicWireSourceCodeForDelegate(delegateType);
        }

        /// <summary>
        /// Generates source code for dynamic wiring of a specified server component delegate property.
        /// </summary>
        /// <param name="delegateType">Delegate type</param>
        /// <returns>Generated C# source code</returns>
        private string CreateDynamicWireSourceCodeForDelegate(Type delegateType)
        {
            MethodInfo delegateMethod = delegateType.GetMethod("Invoke");

            StringBuilder code = new StringBuilder();
            code.AppendLine("namespace Zyan.Communication");
            code.AppendLine("{");
            code.AppendLine("   public class DynamicWire");
            code.AppendLine("   {");
            code.AppendLine("       private DelegateInterceptor _interceptor = null;");
            code.AppendLine("       public DelegateInterceptor Interceptor { get {return _interceptor;} set { _interceptor = value; } }");

            bool hasReturnValue = !delegateMethod.ReturnType.Name.Equals("Void");

            if (!hasReturnValue)
                code.Append("       public void In(");
            else
                code.AppendFormat("       public {0} In(", ScriptEngine.GetCSharpNameOfType(delegateMethod.ReturnType));

            int argCount = 0;
            ParameterInfo[] argInfos = delegateMethod.GetParameters();

            foreach (ParameterInfo argInfo in argInfos)
            {
                argCount++;

                code.AppendFormat("{0} {1}", ScriptEngine.GetCSharpNameOfType(argInfo.ParameterType), argInfo.Name);

                if (argCount < argInfos.Length)
                    code.Append(", ");
            }
            code.Append(")");
            code.AppendLine();
            code.AppendLine("       {");
            code.Append("           ");

            if (!hasReturnValue)
                code.Append("Interceptor.InvokeClientDelegate(");
            else
                code.AppendFormat("return ({0})Interceptor.InvokeClientDelegate(", ScriptEngine.GetCSharpNameOfType(delegateMethod.ReturnType));

            argCount = 0;

            foreach (ParameterInfo argInfo in argInfos)
            {
                argCount++;
                code.Append(argInfo.Name);

                if (argCount < argInfos.Length)
                    code.Append(", ");
            }
            code.AppendLine(");");
            code.AppendLine("       }");
            code.AppendLine("   }");
            code.AppendLine("}");

            return code.ToString();
        }
    }
}

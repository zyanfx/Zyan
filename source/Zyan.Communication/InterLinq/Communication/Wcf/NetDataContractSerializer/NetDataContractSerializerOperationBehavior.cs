// using System;
// using System.Collections.Generic;
// using System.Runtime.Serialization;
// using System.ServiceModel.Description;
// using System.Xml;
//
// namespace Zyan.InterLinq.Communication.Wcf.NetDataContractSerializer
// {
// 	/// <summary>
// 	/// Represents the run-time behavior of the <see cref="DataContractSerializer"/>.
// 	/// </summary>
// 	internal class NetDataContractSerializerOperationBehavior : DataContractSerializerOperationBehavior
// 	{
// 		/// <summary>
// 		/// Initializes a new instance of the 
// 		/// <see cref="DataContractSerializerOperationBehavior"/>
// 		/// class with the specified operation.
// 		/// </summary>
// 		/// <param name="operationDescription">An <see cref="OperationDescription"/> that represents the operation.</param>
// 		public NetDataContractSerializerOperationBehavior(OperationDescription operationDescription) :
// 			base(operationDescription) { }
//
// 		/// <summary>
// 		/// Creates an instance of a class that inherits from <see cref="XmlObjectSerializer"/> 
// 		/// for serialization and deserialization operations.
// 		/// </summary>
// 		/// <param name="type">The <see cref="Type"/> to create the serializer for.</param>
// 		/// <param name="name">The name of the generated <see cref="Type"/>.</param>
// 		/// <param name="ns">The namespace of the generated <see cref="Type"/>.</param>
// 		/// <param name="knownTypes">An <see cref="IList{T}"/> of <see cref="Type"/> that contains known types.</param>
// 		/// <returns>An instance of a class that inherits from the <see cref="XmlObjectSerializer"/> class.</returns>
// 		public override XmlObjectSerializer CreateSerializer(Type type, string name, string ns, IList<Type> knownTypes)
// 		{
// 			return new System.Runtime.Serialization.NetDataContractSerializer();
// 		}
//
// 		/// <summary>
// 		/// Creates an instance of a class that inherits from <see cref="XmlObjectSerializer"/>
// 		/// for serialization and deserialization operations with an <see cref="XmlDictionaryString"/>
// 		/// that contains the namespace.
// 		/// </summary>
// 		/// <param name="type">The <see cref="Type"/> to create the serializer for.</param>
// 		/// <param name="name">The name of the generated <see cref="Type"/>.</param>
// 		/// <param name="ns">An <see cref="XmlDictionaryString"/> that contains the namespace of the serialized <see cref="Type"/>.</param>
// 		/// <param name="knownTypes">An <see cref="IList{T}"/> of <see cref="Type"/> that contains known types.</param>
// 		/// <returns>An instance of a class that inherits from the <see cref="XmlObjectSerializer"/> class.</returns>
// 		public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
// 		{
// 			return new System.Runtime.Serialization.NetDataContractSerializer();
// 		}
// 	}
// }

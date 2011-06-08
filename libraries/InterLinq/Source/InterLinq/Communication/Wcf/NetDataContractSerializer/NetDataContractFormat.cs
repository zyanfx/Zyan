using System;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;

namespace InterLinq.Communication.Wcf.NetDataContractSerializer
{
    /// <summary>
    /// Implements methods that can be used to extend run-time behavior for an operation
    /// in either a service or client application.
    /// </summary>
    internal class NetDataContractFormat : Attribute, IOperationBehavior
    {

        /// <summary>
        /// Implement to pass data at runtime to bindings to support custom behavior.
        /// </summary>
        /// <param name="description">
        ///     The operation being examined. Use for examination only. 
        ///     If the operation description is modified, the results are undefined.
        /// </param>
        /// <param name="parameters">The collection of objects that binding elements require to support the behavior.</param>
        public void AddBindingParameters(OperationDescription description, BindingParameterCollection parameters) { }

        /// <summary>
        /// Implements a modification or extension of the client across an operation.
        /// </summary>
        /// <param name="description">
        ///     The operation being examined. Use for examination only. 
        ///     If the operation description is modified, the results are undefined.
        /// </param>
        /// <param name="proxy">
        ///     The run-time object that exposes customization properties for the operation
        ///     described by operationDescription.
        /// </param>
        public void ApplyClientBehavior(OperationDescription description, ClientOperation proxy)
        {
            ReplaceDataContractSerializerOperationBehavior(description);
        }

        /// <summary>
        /// Implements a modification or extension of the service across an operation.
        /// </summary>
        /// <param name="description">
        ///     The operation being examined. Use for examination only. If the operation
        ///     description is modified, the results are undefined.
        /// </param>
        /// <param name="dispatch">
        ///     The run-time object that exposes customization properties for the operation
        ///     described by operationDescription.
        /// </param>
        public void ApplyDispatchBehavior(OperationDescription description, DispatchOperation dispatch)
        {
            ReplaceDataContractSerializerOperationBehavior(description);
        }

        /// <summary>
        /// Implement to confirm that the operation meets some intended criteria.
        /// </summary>
        /// <param name="description">
        ///     The operation being examined. Use for examination only. If the operation
        ///     description is modified, the results are undefined.
        /// </param>
        public void Validate(OperationDescription description) { }

        /// <summary>
        /// Replaces the <see cref="DataContractSerializerOperationBehavior">behaviour</see>
        /// of <paramref name="description"/> with a new instance of
        /// <see cref="NetDataContractSerializerOperationBehavior"/>.
        /// </summary>
        /// <param name="description">
        ///     The <see cref="OperationDescription">description</see> to
        ///     replace the <see cref="DataContractSerializerOperationBehavior">behaviour</see> in.
        /// </param>
        private static void ReplaceDataContractSerializerOperationBehavior(OperationDescription description)
        {
            DataContractSerializerOperationBehavior dcsOperationBehavior = description.Behaviors.Find<DataContractSerializerOperationBehavior>();

            if (dcsOperationBehavior != null)
            {
                description.Behaviors.Remove(dcsOperationBehavior);
                description.Behaviors.Add(new NetDataContractSerializerOperationBehavior(description));
            }
        }

    }
}

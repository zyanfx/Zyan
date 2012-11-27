using System.Collections.Generic;
using Zyan.Communication.Transport;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Describes client side communication protocol settings.
	/// </summary>
	public interface IClientProtocolSetup
	{
		/// <summary>
		/// Gets a list of all stages of the send pipeline.
		/// </summary>
		List<ISendPipelineStage> SendPipeline { get; }

		/// <summary>
        /// Gets a list of all stages of the receive pipeline.
		/// </summary>
		List<IReceivePipelineStage> ReceivePipeline { get; }

		/// <summary>
		/// Creates and configures a transport channel.
		/// </summary>
		/// <returns>Transport channel</returns>
		IZyanTransportChannel CreateChannel();

		/// <summary>
		/// Gets a dictionary with channel settings.
		/// </summary>
		Dictionary<string, object> ChannelSettings { get; }
	}
}

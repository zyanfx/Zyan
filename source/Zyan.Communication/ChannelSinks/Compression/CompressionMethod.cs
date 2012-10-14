namespace Zyan.Communication.ChannelSinks.Compression
{
	/// <summary>
	/// Compression levels.
	/// </summary>
	public enum CompressionMethod
	{
		/// <summary>
		/// No compression.
		/// </summary>
		None,

		/// <summary>
		/// LZF: very fast compression, poor ratio.
		/// </summary>
		LZF,

		/// <summary>
		/// DeflateStream: slower compression, better ratio.
		/// </summary>
		DeflateStream,

		/// <summary>
		/// Default compression method is DeflateStream.
		/// </summary>
		Default = DeflateStream
	}
}

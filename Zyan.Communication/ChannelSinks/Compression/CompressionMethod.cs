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
		/// Fast compression, poor ratio (LZF).
		/// </summary>
		Fast,

		/// <summary>
		/// Average compression, better ratio (DeflateStream).
		/// </summary>
		Average,

		/// <summary>
		/// Default compression level is Average.
		/// </summary>
		Default = Average
	}
}

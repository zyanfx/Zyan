namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// Hash function signature.
	/// Computes the hash of the specified <see cref="string"/> or <see cref="SrpInteger"/> values.
	/// </summary>
	/// <param name="values">The values.</param>
	public delegate SrpInteger SrpHash(params object[] values);

	/// <summary>
	/// Interface for the hash functions used by SRP-6a protocol.
	/// </summary>
	public interface ISrpHash
	{
		/// <summary>
		/// Gets the hashing function.
		/// </summary>
		SrpHash HashFunction { get; }

		/// <summary>
		/// Gets the hash size in bytes.
		/// </summary>
		int HashSizeBytes { get; }
	}
}

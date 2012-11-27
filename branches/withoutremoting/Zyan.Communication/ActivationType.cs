namespace Zyan.Communication
{
	/// <summary>
	/// Enumeration of supported activation types.
	/// </summary>
	public enum ActivationType : short
	{
		/// <summary>
		/// Component instance lives only for a single call.
		/// <remarks>Single call activated components need not be thread-safe</remarks>
		/// </summary>
		SingleCall = 1,

		/// <summary>
		/// Component instance is created on first call and reused for all subsequent calls.
		/// <remarks>Singleton activated components must be thread-safe</remarks>
		/// </summary>
		Singleton
	}
}
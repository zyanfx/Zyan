namespace Util
{
    /// <summary>
    /// The ICompressible interface implements a method that returns a flag,
    /// which determines if the request or response should be compressed.
    /// This interface is used in conjuction with the compression sink implementation
    /// and allows to determine dynamically if the request or response is
    /// to be compressed.
    /// </summary>
    /// <remarks>
    /// The following is the order, in which the criteria are evaluated to determine
    /// if the request or response is to be compressed: Threshold should be greater than
    /// zero. NonCompressible marks the object as an exempt. If object size is
    /// greater than threshold and not marked as NonCompressible, the ICompressible is evaluated.
    /// </remarks>
    public interface ICompressible
    {
        /// <summary>
        /// Implement a method in the class. Return true, if the compression
        /// </summary>
        /// <returns>True if the object should be compressed, otherwise not.</returns>
        bool PerformCompression();
    }
}

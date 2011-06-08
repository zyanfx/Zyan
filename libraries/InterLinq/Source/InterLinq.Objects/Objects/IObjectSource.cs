using System;
using System.Collections.Generic;

namespace InterLinq.Objects
{
    /// <summary>
    /// This interface contains methods to get all <see langword="object">objects</see>
    /// of a certain <see cref="Type"/>.
    /// </summary>
    public interface IObjectSource
    {
        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> containing all objects
        /// of the <see cref="Type"/> <typeparamref name="T"/> stored in
        /// this implementation of <see cref="IObjectSource"/>.
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of the <see langword="object">objects</see>.</typeparam>
        /// <returns>
        /// Returns an <see cref="IEnumerable{T}"/> containing all objects
        /// of the <see cref="Type"/> <typeparamref name="T"/> stored in
        /// this implementation of <see cref="IObjectSource"/>.
        /// </returns>
        /// <remarks>
        /// The implementation of this method may throws an <see cref="Exception"/> if
        /// the requested <see cref="Type"/> could not be found.
        /// </remarks>
        IEnumerable<T> GetObjects<T>();
    }
}

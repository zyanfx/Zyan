using System;
using System.Collections.Generic;
using InterLinq.Examples.Simple.Artefacts;
using InterLinq.Objects;

namespace InterLinq.Examples.Simple.Server
{
    /// <summary>
    /// This class is a <see cref="IObjectSource"/> implementation which contains
    /// <see cref="SimpleObject">SimpleObjects</see>.
    /// </summary>
    public class ExampleObjectSource : IObjectSource
    {

        ///<summary>
        /// Initializes this class.
        ///</summary>
        public ExampleObjectSource()
        {
            SimpleObjects = new List<SimpleObject>();
        }

        /// <summary>
        /// A list of all stored <see cref="SimpleObject">SimpleObjects</see>.
        /// </summary>
        public List<SimpleObject> SimpleObjects { get; set; }

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
        /// <exception cref="Exception">
        /// Thorw an Exception if the type <typeparamref name="T"/> is not stored in this <see cref="ExampleObjectSource"/>.
        /// </exception>
        public IEnumerable<T> GetObjects<T>()
        {
            if (typeof(T) == typeof(SimpleObject))
            {
                return (IEnumerable<T>)SimpleObjects;
            }
            throw new Exception(string.Format("Type '{0}' is not stored in this ExampleObjectSource.", typeof(T)));
        }

    }
}

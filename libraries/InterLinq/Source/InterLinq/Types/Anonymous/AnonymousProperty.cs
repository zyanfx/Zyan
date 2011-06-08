using System;
using System.Runtime.Serialization;

namespace InterLinq.Types.Anonymous
{
    /// <summary>
    /// Represents an instance of an <see cref="AnonymousMetaProperty"/>.
    /// </summary>
    [Serializable]
    [DataContract]
    public class AnonymousProperty
    {

        #region Properties

        /// <summary>
        /// The name of the property.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// The value of the property.
        /// </summary>
        [DataMember]
        public object Value { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public AnonymousProperty() { }

        /// <summary>
        /// Instance a new instance of the class <see cref="AnonymousProperty"/> and initialize it.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="value">Value of the property.</param>
        public AnonymousProperty(string name, object value)
        {
            Name = name;
            Value = value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a <see langword="string"/> representing this object.
        /// </summary>
        /// <returns>Returns a <see langword="string"/> representing this object.</returns>
        public override string ToString()
        {
            return string.Format("{0} = {1}", Name, Value);
        }

        #endregion

    }
}

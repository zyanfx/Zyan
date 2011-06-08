using System.Runtime.Serialization;

namespace InterLinq.Examples.Simple.Artefacts
{
    /// <summary>
    /// This class is a simple object with a name and <see langword="int"/> value.
    /// </summary>
    [DataContract]
    public class SimpleObject
    {

        /// <summary>
        /// Name of the <see cref="SimpleObject"/>.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// An integer value.
        /// </summary>
        [DataMember]
        public int Value { get; set; }

    }
}

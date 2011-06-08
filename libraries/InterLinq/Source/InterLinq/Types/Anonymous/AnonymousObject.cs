using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace InterLinq.Types.Anonymous
{
    /// <summary>
    /// Represents an instance of an <see cref="AnonymousMetaType"/>.
    /// </summary>
    [Serializable]
    [DataContract]
    public class AnonymousObject
    {

        #region Properties

        /// <summary>
        /// The properties of the instance.
        /// </summary>
        [DataMember]
        public List<AnonymousProperty> Properties { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Instance a new instance of the class <see cref="AnonymousObject"/>.
        /// </summary>
        public AnonymousObject()
        {
            Properties = new List<AnonymousProperty>();
        }

        /// <summary>
        /// Instance a new instance of the class <see cref="AnonymousObject"/>
        /// and initialze it with a list of properties.
        /// </summary>
        /// <param name="properties"><see cref="AnonymousProperty">Anonymous properties</see> to add.</param>
        public AnonymousObject(IEnumerable<AnonymousProperty> properties)
        {
            Properties = new List<AnonymousProperty>();
            Properties.AddRange(properties);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a string representing this <see cref="AnonymousObject"/>.
        /// </summary>
        /// <returns>Returns a string representing this <see cref="AnonymousObject"/>.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{ ");
            bool first = true;
            foreach (AnonymousProperty property in Properties)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(property.ToString());
            }
            sb.Append(" }");
            return sb.ToString();
        }

        #endregion

    }
}

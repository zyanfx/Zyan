using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.HotUpdate
{
    [Serializable]
    public class Envelope 
    {
        /// <summary>
        /// Gets or sets the target application name.
        /// </summary>
        public string TargetApplicationName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the message data as BLOB.
        /// </summary>
        public byte[] MessageBlob
        {
            get;
            set;
        }
    }
}

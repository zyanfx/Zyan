using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Zyan.Communication
{
    /// <summary>
    /// Describes arguments for NewLogonNeeded events.
    /// </summary>
    [Serializable]
    public class NewLogonNeededEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of the NewLogonNeededEventArgs class.
        /// </summary>
        public NewLogonNeededEventArgs()
        {
            Cancel = false;
        }

        /// <summary>
        /// Creates a new instance of the NewLogonNeededEventArgs class.
        /// </summary>
        /// <param name="credentials">Credentials for logon</param>
        public NewLogonNeededEventArgs(Hashtable credentials)
        {
            Cancel = false;
            Credentials = credentials;
        }

        /// <summary>
        /// Creates a new instance of the NewLogonNeededEventArgs class.
        /// </summary>
        /// <param name="credentials">Security credentials</param>
        /// <param name="cancel">Cancel flag</param>
        public NewLogonNeededEventArgs(Hashtable credentials, bool cancel)
        {
            Cancel = cancel;
            Credentials = credentials;
        }

        /// <summary>
        /// Gets or sets the security credentials for the new logon.
        /// </summary>
        public Hashtable Credentials { get; set; }

        /// <summary>
        /// Gets or sets the cancel flag. If set to true, the new logon will be canceled.
        /// </summary>
        public bool Cancel { get; set; }
    }
}

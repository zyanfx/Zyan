using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.SessionMgmt
{
    public class SessionVariableAdapter
    {
        private ISessionManager _sessionManager = null;

        private Guid _sessionID = Guid.Empty;

        internal SessionVariableAdapter(ISessionManager sessionManager, Guid sessionID)
        {
            _sessionManager = sessionManager;
            _sessionID = sessionID;
        }

        /// <summary>
        /// Legt den Wert einer Sitzungsvariablen fest.
        /// </summary>        
        /// <param name="name">Variablenname</param>
        /// <param name="value">Wert</param>
        public void SetSessionVariable(string name, object value)
        {
            _sessionManager.SetSessionVariable(_sessionID, name, value);
        }

        /// <summary>
        /// Gibt den Wert einer Sitzungsvariablen zurück.
        /// </summary>        
        /// <param name="name">Variablenname</param>
        /// <returns>Wert</returns>
        public object GetSessionVariable(string name)
        {
            return _sessionManager.GetSessionVariable(_sessionID, name);
        }
    }
}

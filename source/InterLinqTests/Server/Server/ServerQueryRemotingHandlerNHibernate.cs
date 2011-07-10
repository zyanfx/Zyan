using System;
using InterLinq.Communication.Remoting;

namespace InterLinq.UnitTests.Server
{
    public class ServerQueryRemotingHandlerNHibernate : ServerQueryRemotingHandler
    {

        /// <summary>
        /// Initializes this class.
        /// </summary>
        /// <param name="innerHandler">Inner Handler of this Server.</param>
        public ServerQueryRemotingHandlerNHibernate(IQueryHandler innerHandler) : base(innerHandler) { }
        protected ServerQueryRemotingHandlerNHibernate() { }

        private static IQueryRemoteHandler registeredService;

        protected override void RegisterService(IQueryRemoteHandler serviceHandler)
        {
            registeredService = serviceHandler;
        }

        protected override IQueryRemoteHandler GetRegisteredService()
        {
            if (registeredService == null)
            {
                throw new Exception("Service could not be found.");
            }
            return registeredService;
        }
    }
}

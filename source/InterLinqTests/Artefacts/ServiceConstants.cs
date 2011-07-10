using System;

namespace InterLinq.UnitTests.Artefacts
{
    public abstract class ServiceConstants
    {

        public const string NhibernateServiceName = "InterLinqServiceNHibernate";
        public const string SqlServiceName = "InterLinqServiceSQL";
        public const string ObjectsServiceName = "InterLinqServiceObject";
        public const string EntityFramework4ServiceName = "InterLinqServiceEntityFramework4";

        public const int NhibernatePort = 7891;
        public const int SqlPort = 7892;
        public const int ObjectsPort = 7893;
        public const int EntityFramework4Port = 7894;

        /// <summary>
        /// Returns the of a specific service name.
        /// </summary>
        /// <param name="serviceName">Name of the service to find the port.</param>
        /// <returns>Returns the port of the service or throws an <see cref="Exception"/>.</returns>
        public static int GetServicePort(string serviceName)
        {
            if (serviceName == NhibernateServiceName)
            {
                return NhibernatePort;
            }
            if (serviceName == SqlServiceName)
            {
                return SqlPort;
            }
            if (serviceName == ObjectsServiceName)
            {
                return ObjectsPort;
            }
            if (serviceName == EntityFramework4ServiceName)
            {
                return EntityFramework4Port;
            }
            throw new Exception("Service port could not be found.");
        }
    }
}

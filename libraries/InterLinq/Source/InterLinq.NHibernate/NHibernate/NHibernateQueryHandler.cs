using System;
using System.Linq;
using System.Reflection;
using NHibernate;
using NHibernate.Linq;

namespace InterLinq.NHibernate
{
    /// <summary>
    /// LINQ for NHibernate specific implementation of the
    /// <see cref="IQueryHandler"/>.
    /// </summary>
    /// <seealso cref="IQueryHandler"/>
    public class NHibernateQueryHandler : IQueryHandler
    {

        #region Fields

        private readonly ISessionFactory sessionFactory;
        private ISession currentSession;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="ISession">CurrentSession</see>.
        /// </summary>
        private ISession CurrentSession
        {
            get
            {
                if (currentSession == null)
                {
                    StartSession();
                }
                return currentSession;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes this class.
        /// </summary>
        /// <param name="sessionFactory"><see cref="ISessionFactory"/> used for session initialization / disposal.</param>
        public NHibernateQueryHandler(ISessionFactory sessionFactory)
        {
            if (sessionFactory == null)
            {
                throw new ArgumentNullException("sessionFactory");
            }
            this.sessionFactory = sessionFactory;
        }

        #endregion

        #region IQueryHandler Members

        /// <summary>
        /// Returns an <see cref="IQueryable{T}"/>.
        /// </summary>
        /// <typeparam name="T">Generic Argument of the returned <see cref="IQueryable{T}"/>.</typeparam>
        /// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
        /// <seealso cref="IQueryHandler.Get{T}"/>
        public IQueryable<T> GetTable<T>() where T : class
        {
            return Get<T>();
        }

        /// <summary>
        /// Returns an <see cref="IQueryable{T}"/>.
        /// </summary>
        /// <param name="type">Type of the returned <see cref="IQueryable{T}"/>.</param>
        /// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
        /// <seealso cref="IQueryHandler.Get"/>
        public IQueryable GetTable(Type type)
        {
            return Get(type);
        }

        /// <summary>
        /// Returns an <see cref="IQueryable{T}"/>.
        /// </summary>
        /// <param name="type">Type of the returned <see cref="IQueryable{T}"/>.</param>
        /// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
        public IQueryable Get(Type type)
        {
            MethodInfo getTableMethod = GetType().GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
            MethodInfo genericGetTableMethod = getTableMethod.MakeGenericMethod(type);
            return (IQueryable)genericGetTableMethod.Invoke(this, new object[0]);
        }

        /// <summary>
        /// Returns an <see cref="IQueryable{T}"/>.
        /// </summary>
        /// <typeparam name="T">Generic Argument of the returned <see cref="IQueryable{T}"/>.</typeparam>
        /// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
        public IQueryable<T> Get<T>() where T : class
        {
            return CurrentSession.Linq<T>();
        }

        /// <summary>
        /// Tells the <see cref="IQueryHandler"/> to start a new the session.
        /// </summary>
        /// <returns>True, if the session creation was successful. False, if not.</returns>
        /// <seealso cref="IQueryHandler.StartSession"/>
        public bool StartSession()
        {
            if (currentSession == null)
            {
                currentSession = sessionFactory.OpenSession();
            }
            return true;
        }

        /// <summary>
        /// Tells the <see cref="IQueryHandler"/> to close the current session.
        /// </summary>
        /// <returns>True, if the session closing was successful. False, if not.</returns>
        /// <seealso cref="IQueryHandler.CloseSession"/>
        public bool CloseSession()
        {
            if (currentSession != null)
            {
                currentSession.Close();
                currentSession = null;
            }
            return true;
        }

        #endregion

    }
}

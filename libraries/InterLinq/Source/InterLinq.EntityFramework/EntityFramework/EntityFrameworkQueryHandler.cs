using System;
using System.Data.Objects;
using System.Linq;
using System.Reflection;

namespace InterLinq.EntityFramework
{
    /// <summary>
    /// LINQ to Entities specific implementation of the
    /// <see cref="IQueryHandler"/>.
    /// </summary>
    /// <seealso cref="IQueryHandler"/>
    public class EntityFrameworkQueryHandler : IQueryHandler
    {

        #region Fields

        private readonly ObjectContext objectContext;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes this class.
        /// </summary>
        /// <param name="objectContext">A entity <see cref="ObjectContext"/>.</param>
        public EntityFrameworkQueryHandler(ObjectContext objectContext)
        {
            if (objectContext == null)
            {
                throw new ArgumentNullException("objectContext");
            }
            this.objectContext = objectContext;
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
            return objectContext.CreateQuery<T>(string.Format("[{0}]", typeof(T).Name));
        }

        /// <summary>
        /// Tells the <see cref="IQueryHandler"/> to start a new the session.
        /// </summary>
        /// <returns>True, if the session creation was successful. False, if not.</returns>
        /// <seealso cref="IQueryHandler.StartSession"/>
        public bool StartSession()
        {
            return true;
        }

        /// <summary>
        /// Tells the <see cref="IQueryHandler"/> to close the current session.
        /// </summary>
        /// <returns>True, if the session closing was successful. False, if not.</returns>
        /// <seealso cref="IQueryHandler.CloseSession"/>
        public bool CloseSession()
        {
            return true;
        }

        #endregion
    }
}

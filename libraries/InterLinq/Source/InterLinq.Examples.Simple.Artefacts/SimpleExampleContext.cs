using System.Linq;

namespace InterLinq.Examples.Simple.Artefacts
{
    /// <summary>
    /// This is a context for easier access to the <see cref="IQueryHandler"/>.
    /// </summary>
    public class SimpleExampleContext : InterLinqContext
    {

        /// <summary>
        /// A simple constructor with a <see cref="IQueryHandler"/>.
        /// </summary>
        /// <param name="queryHandler">The <see cref="IQueryHandler"/> to retrieve <see cref="IQueryable">querables</see> from.</param>
        public SimpleExampleContext(IQueryHandler queryHandler)
            : base(queryHandler)
        {
        }

        /// <summary>
        /// Gets all <see cref="SimpleObject">simple objects</see>.
        /// </summary>
        public IQueryable<SimpleObject> SimpleObjects
        {
            get { return QueryHander.Get<SimpleObject>(); }
        }

    }
}

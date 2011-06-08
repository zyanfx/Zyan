
namespace InterLinq.UnitTests.Server
{
    public abstract class TestServer
    {

        #region Fields

        protected string connectionString;

        #endregion

        #region Properties

        protected string databaseName;
        public abstract string DatabaseName { get; }

        protected string createScriptName;
        public abstract string CreateScriptName { get; }

        protected string integrityScriptName;
        public abstract string IntegrityScriptName { get; }

        #endregion

        #region Methods

        public abstract void Start();
        public abstract void Publish();

        #endregion

    }
}

using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Security.Principal;

namespace Zyan.Communication.SessionMgmt
{
    /// <summary>
    /// Komponenten für Sitzungsverwaltung in einer SQL Server-Datenbank.
    /// <remarks>
    /// Folgendes Tabellenschema wird vorausgesetzt:
    /// 
    /// SessionID           uniqueidentifier
    /// SessionTimestamp    datetime
    /// IdentityName        nvarchar(255)
    /// </remarks>
    /// </summary>
    public class SqlSessionManager : ISessionManager, IDisposable
    {
        // Verbindungszeichenfolge
        private string _connectionString = string.Empty;

        // SQL-Schema
        private string _sqlSchema = string.Empty;

        // Tabellenname
        private string _sqlTableName = string.Empty;
        
        /// <summary>
        /// Erzeugt eine neue Instanz von InProcSessionManager.
        /// </summary>
        /// <param name="connectionString">Verbindungszeichenfolge zur SQL Server Datenbank</param>
        /// <param name="sqlSchema">Name des Datenbankschemas (z.B.: "dbo")</param>
        /// <param name="sqlTableName">Name der Sitzungstabelle</param>
        public SqlSessionManager(string connectionString, string sqlSchema, string sqlTableName)
        {
            // Felder füllen
            _connectionString = connectionString;
            _sqlSchema = sqlSchema;
            _sqlTableName = sqlTableName;

            // Sicherstellen, dass die Sitzungstabelle existier
            EnsureSessionTableCreated();

            // Aufräumvorgang starten
            StartSessionSweeper();
        }

        #region SQL Server-Persistenz

        private bool ExistSessionTable()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.Append("SELECT @tableCount=COUNT([object_id]) ");
                sqlBuilder.Append("FROM sys.tables ");
                sqlBuilder.Append("WHERE name = @tableName AND schema_id = SCHEMA_ID(@schemaName)");
                
                using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(),connection))
                {
                    // Ausgbeparamater erzeugen
                    SqlParameter tableCountParam = new SqlParameter("@tableCount", SqlDbType.Int);
                    tableCountParam.Direction = ParameterDirection.Output;
                    command.Parameters.Add(tableCountParam);

                    // Eingabeparameter erzeugen
                    command.Parameters.Add("@tableName", SqlDbType.NVarChar, 255).Value = _sqlTableName;
                    command.Parameters.Add("@schemaName", SqlDbType.NVarChar, 255).Value = _sqlSchema;

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                    return ((int)tableCountParam.Value) == 1;
                }
            }
        }

        private void EnsureSessionTableCreated()
        {
            if (!ExistSessionTable())
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.Append("CREATE TABLE ");
                    sqlBuilder.AppendFormat("[{0}].[{1}] (", _sqlSchema, _sqlTableName);                    
                    sqlBuilder.Append("[AutoID] int IDENTITY(1,1) NOT NULL,");
                    sqlBuilder.Append("[SessionID] uniqueidentifier NOT NULL,");
                    sqlBuilder.Append("[SessionTimestamp] datetime NOT NULL,");
                    sqlBuilder.Append("[IdentityName] nvarchar(255) NOT NULL,");
                    sqlBuilder.AppendFormat("CONSTRAINT [PK_{0}_SessionID] PRIMARY KEY NONCLUSTERED ",_sqlTableName);
                    sqlBuilder.Append("([SessionID] ASC),");
                    sqlBuilder.AppendFormat("CONSTRAINT [IX_{0}] UNIQUE CLUSTERED ",_sqlTableName);
                    sqlBuilder.Append("([AutoID] ASC))");

                    using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
            }
        }

        private ServerSession GetSessionFromSqlServer(Guid sessionID)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.Append("SELECT SessionID,SessionTimestamp,IdentityName ");
                sqlBuilder.AppendFormat("FROM [{0}].[{1}] ",_sqlSchema,_sqlTableName);
                sqlBuilder.Append("WHERE SessionID=@sessionID");

                using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                {
                    // Eingabeparameter erzeugen
                    command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

                    try
                    {
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Sitzung vom SQL Server lesen und zurückgeben
                                return new ServerSession(reader.GetGuid(0),
                                                         reader.GetDateTime(1),
                                                         new GenericIdentity(reader.GetString(2)));
                            }
                            else
                                // Nichts zurückgeben
                                return null;
                        }
                    }
                    finally
                    {   
                        connection.Close();
                    }
                }
            }            
        }

        private bool ExistSessionOnSqlServer(Guid sessionID)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.Append("SELECT @sessionCount=COUNT(SessionID)");
                sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlTableName);
                sqlBuilder.Append("WHERE SessionID=@sessionID");

                using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                {
                    // Eingabeparameter erzeugen
                    command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

                    // Ausgabeparameter erzeugen
                    SqlParameter sessionCountParam = new SqlParameter()
                    {
                        ParameterName = "@sessionCount",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(sessionCountParam);

                    // SQL-Befehl ausführen
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                    // Ergebnis zurückgeben
                    return ((int)sessionCountParam.Value) == 1;
                }
            }            
        }

        private void StoreSessionOnSqlServer(ServerSession session)
        {
            // Wenn keine Sitzung angegeben wurde ...
            if (session == null)
                // Ausnahme werfen
                throw new ArgumentNullException("session");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] WHERE SessionID=@sessionID; ", _sqlSchema, _sqlTableName);
                sqlBuilder.AppendFormat("INSERT [{0}].[{1}] (SessionID,SessionTimestamp,IdentityName) ", _sqlSchema, _sqlTableName);
                sqlBuilder.Append("VALUES (@sessionID,@sessionTimestamp,@identityName)");
                
                using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                {
                    // Eingabeparameter erzeugen
                    command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = session.SessionID;
                    command.Parameters.Add("@sessionTimestamp", SqlDbType.DateTime).Value = session.Timestamp;
                    command.Parameters.Add("@identityName", SqlDbType.NVarChar,255).Value = session.Identity.Name;

                    // Sitzung speichern
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }            
        }

        private void RemoveSessionFromSqlServer(Guid sessionID)
        {            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] WHERE SessionID=@sessionID", _sqlSchema, _sqlTableName);
                
                using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                {
                    // Eingabeparameter erzeugen
                    command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;
                    
                    // Sitzung speichern
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        private void SweepExpiredSessionsFromSqlServer()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] ", _sqlSchema, _sqlTableName);
                sqlBuilder.Append("WHERE DATEDIFF(minute,SessionTimestamp,GETDATE())>@sessionAgeLimit");

                using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                {
                    // Eingabeparameter erzeugen
                    command.Parameters.Add("@sessionAgeLimit", SqlDbType.Int).Value = _sessionAgeLimit;

                    // Sitzung speichern
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        #endregion

        /// <summary>
        /// Prüft, ob eine Sitzung mit einer bestimmten Sitzungskennung.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <returns>Wahr, wenn die Sitzung existiert, ansonsten Falsch</returns>
        public bool ExistSession(Guid sessionID)
        {
            return ExistSessionOnSqlServer(sessionID);
        }

        /// <summary>
        /// Gibt eine bestimmte Sitzung zurück.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        /// <returns>Sitzung</returns>
        public ServerSession GetSessionBySessionID(Guid sessionID)
        {            
            // Sitzung abrufen und zurückgeben
            return GetSessionFromSqlServer(sessionID);
        }

        /// <summary>
        /// Speichert eine Sitzung.
        /// </summary>
        /// <param name="session">Sitzungsdaten</param>
        public void StoreSession(ServerSession session)
        {   
            // Sitzung der Sitzungsliste zufüen
            StoreSessionOnSqlServer(session);                            
        }

        /// <summary>
        /// Löscht eine bestimmte Sitzung.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        public void RemoveSession(Guid sessionID)
        {   
            // Sitzung aus der Sitzungsliste entfernen
            RemoveSessionFromSqlServer(sessionID);            
        }

        #region Aufräumvorgang

        // Zeitgeber für Sitzungs-Aufräumvorgang
        private System.Timers.Timer _sessionSweeper = null;

        // Sperrobjekte für Thread-Synchronisierung        
        private object _sessionSweeperLock = new object();

        // Maximale Sitzungslebensdauer in Minuten
        private int _sessionAgeLimit = 240;

        // Intervall für den Sitzungs-Aufräumvorgang in Minuten
        private int _sessionSweepInterval = 15;

        /// <summary>
        /// Gibt die maximale Sitzungslebensdauer (in Minuten) zurück oder legt sie fest.
        /// </summary>
        public int SessionAgeLimit
        {
            get { return _sessionAgeLimit; }
            set { _sessionAgeLimit = value; }
        }

        /// <summary>
        /// Gibt den Intervall für den Sitzungs-Aufräumvorgang (in Minuten) zurück oder legt ihn fest.
        /// </summary>
        public int SessionSweepInterval
        {
            get { return _sessionSweepInterval; }
            set
            {
                // Zeitgeber stoppen
                StopSessionSweeper();

                // Intervall einstellen
                _sessionSweepInterval = value;

                // Zeitgeber starten
                StartSessionSweeper();
            }
        }

        /// <summary>
        /// Startet den Zeitgeber für den Sitzungs-Aufräumvorgang.
        /// </summary>
        private void StartSessionSweeper()
        {
            lock (_sessionSweeperLock)
            {
                // Wenn der Zeitgeber noch nicht existiert ...
                if (_sessionSweeper == null)
                {
                    // Zeitgeber für Sitzungs-Aufräumvorgang erzeugen
                    _sessionSweeper = new Timer(_sessionSweepInterval * 60000);

                    // Elapsed-Ereignis abonnieren
                    _sessionSweeper.Elapsed += new ElapsedEventHandler(_sessionSweeper_Elapsed);

                    // Zeitgeber starten
                    _sessionSweeper.Start();
                }
            }
        }

        /// <summary>
        /// Stoppt den Zeitgeber für den Sitzungs-Aufräumvorgang.
        /// </summary>
        private void StopSessionSweeper()
        {
            lock (_sessionSweeperLock)
            {
                // Wenn der Zeitgeber existiert ...
                if (_sessionSweeper != null)
                {
                    // Wenn der Zeitgeber läuft ...
                    if (_sessionSweeper.Enabled)
                        // Zeitgeber stopen
                        _sessionSweeper.Stop();

                    // Zeitgeber entsorgen
                    _sessionSweeper.Dispose();
                    _sessionSweeper = null;
                }
            }
        }

        /// <summary>
        /// Bei Intervall abgelaufene Sitzungen löschen.
        /// </summary>
        /// <param name="sender">Herkunftsobjekt</param>
        /// <param name="e">Ereignisargumente</param>
        private void _sessionSweeper_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Abgelaufene Sitzungen auf dem SQL Server bereinigen
            SweepExpiredSessionsFromSqlServer();
        }

        #endregion

        // Gibt an, ob Dispose bereits aufgerufen wurde, oder nicht
        private bool _isDisposed = false;

        /// <summary>
        /// Verwaltete Ressourcen freigeben.
        /// </summary>
        public void Dispose()
        {
            // Wenn Dispose noch nicht aufgerufen wurde ...
            if (!_isDisposed)
            {
                // Schalter setzen
                _isDisposed = true;

                // Wenn der Sitzungs-Aufräumzeitgeber noch existiert ...
                if (_sessionSweeper != null)
                {
                    // Wenn der Zeitgeber noch läuft ...
                    if (_sessionSweeper.Enabled)
                        // Zeitgeber anhalten
                        _sessionSweeper.Stop();

                    // Zeitgeber entsorgen
                    _sessionSweeper.Dispose();
                }                
            }
        }
    }
}

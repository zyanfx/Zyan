using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Text;
using System.Timers;
using System.Security.Principal;
using System.Transactions;

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

        // Tabellennamen
        private string _sqlSessionTableName = string.Empty;
        private string _sqlVariablesTableName = string.Empty;
        
        /// <summary>
        /// Erzeugt eine neue Instanz von InProcSessionManager.
        /// </summary>
        /// <param name="connectionString">Verbindungszeichenfolge zur SQL Server Datenbank</param>
        /// <param name="sqlSchema">Name des Datenbankschemas (z.B.: "dbo")</param>
        /// <param name="sqlSessionTableName">Name der Sitzungstabelle</param>
        /// <param name="sqlVariablesTableName">Name der Tabelle für Sitzungsvariablen</param>
        public SqlSessionManager(string connectionString, string sqlSchema, string sqlSessionTableName, string sqlVariablesTableName)
        {
            // Felder füllen
            _connectionString = connectionString;
            _sqlSchema = sqlSchema;
            _sqlSessionTableName = sqlSessionTableName;
            _sqlVariablesTableName = sqlVariablesTableName;

            // Sicherstellen, dass die Tabellen auf dem SQL Server existieren
            EnsureSessionTableCreated();
            EnsureVariablesTableCreated();

            // Aufräumvorgang starten
            StartSessionSweeper();
        }

        #region SQL Server-Persistenz

        /// <summary>
        /// Gibt zurück, ob eine bestimmte Tabelle in der Verbundenen SQL Server Datenbank bereits angelegt wurde, oder nicht.
        /// </summary>
        /// <param name="schema">SQL Server-Schema (z.B. dbo)</param>
        /// <param name="tableName">Tabellenname</param>
        /// <returns>Wahr, wenn die Tabelle existiert, ansonsten Falsch</returns>
        private bool ExistSqlTable(string schema, string tableName)
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() {IsolationLevel=System.Transactions.IsolationLevel.RepeatableRead}))
            {
                // Verbindung zum SQL Server herstellen
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // SQL-Abfrage zusammenbauen
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.Append("SELECT @tableCount=COUNT([object_id]) ");
                    sqlBuilder.Append("FROM sys.tables ");
                    sqlBuilder.Append("WHERE name = @tableName AND schema_id = SCHEMA_ID(@schemaName)");

                    // SQL-Befehl erzeugen
                    using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                    {
                        // Ausgbeparamater erzeugen
                        SqlParameter tableCountParam = new SqlParameter("@tableCount", SqlDbType.Int);
                        tableCountParam.Direction = ParameterDirection.Output;
                        command.Parameters.Add(tableCountParam);

                        // Eingabeparameter erzeugen
                        command.Parameters.Add("@tableName", SqlDbType.NVarChar, 255).Value = tableName;
                        command.Parameters.Add("@schemaName", SqlDbType.NVarChar, 255).Value = schema;

                        // SQL-Abfrage ausfphren
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();

                        // Transaktion abschließen
                        scope.Complete();

                        // Ausgabeparameter auswerten und Ergebnis zurückgeben
                        return ((int)tableCountParam.Value) == 1;
                    }
                }
            }
        }
        
        /// <summary>
        /// Stellt sicher, dass die Sitzungstabelle in der SQL Server Datenbank existiert.
        /// </summary>
        private void EnsureSessionTableCreated()
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
            {
                // Wenn die Tabelle nicht existiert ...
                if (!ExistSqlTable(_sqlSchema,_sqlSessionTableName))
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        StringBuilder sqlBuilder = new StringBuilder();
                        sqlBuilder.Append("CREATE TABLE ");
                        sqlBuilder.AppendFormat("[{0}].[{1}] (", _sqlSchema, _sqlSessionTableName);
                        sqlBuilder.Append("[AutoID] int IDENTITY(1,1) NOT NULL,");
                        sqlBuilder.Append("[SessionID] uniqueidentifier NOT NULL,");
                        sqlBuilder.Append("[SessionTimestamp] datetime NOT NULL,");
                        sqlBuilder.Append("[IdentityName] nvarchar(255) NOT NULL,");
                        sqlBuilder.AppendFormat("CONSTRAINT [PK_{0}_SessionID] PRIMARY KEY NONCLUSTERED ", _sqlSessionTableName);
                        sqlBuilder.Append("([SessionID] ASC),");
                        sqlBuilder.AppendFormat("CONSTRAINT [IX_{0}] UNIQUE CLUSTERED ", _sqlSessionTableName);
                        sqlBuilder.Append("([AutoID] ASC))");

                        using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
                // Transaktion abschließen
                scope.Complete();
            }
        }

        /// <summary>
        /// Stellt sicher, dass die Variablentabelle in der SQL Server Datenbank existiert.
        /// </summary>
        private void EnsureVariablesTableCreated()
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
            {
                // Wenn die Tabelle nicht existiert ...
                if (!ExistSqlTable(_sqlSchema, _sqlVariablesTableName))
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        StringBuilder sqlBuilder = new StringBuilder();
                        sqlBuilder.Append("CREATE TABLE ");
                        sqlBuilder.AppendFormat("[{0}].[{1}] (", _sqlSchema, _sqlVariablesTableName);                        
                        sqlBuilder.Append("[SessionID] uniqueidentifier NOT NULL,");
                        sqlBuilder.Append("[VariableName] nvarchar(255) NOT NULL,");
                        sqlBuilder.Append("[VariableValue] varbinary(MAX) NULL,");
                        sqlBuilder.AppendFormat("CONSTRAINT [PK_{0}_SessionID_VariableName] PRIMARY KEY ", _sqlSessionTableName);
                        sqlBuilder.Append("([SessionID], [VariableName]))");
                        
                        using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
                // Transaktion abschließen
                scope.Complete();
            }
        }

        /// <summary>
        /// Ruft eine bestimmte Sitzung aus der SQL Server Datenbank ab.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <returns>Sitzungsobjekt</returns>
        private ServerSession GetSessionFromSqlServer(Guid sessionID)
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
            {
                // Verbindung zum SQL Server herstellen
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // SQL-Abfrage zusammenstellen
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.Append("SELECT SessionID,SessionTimestamp,IdentityName ");
                    sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlSessionTableName);
                    sqlBuilder.Append("WHERE SessionID=@sessionID");

                    // SQL-Befehl erzeugen
                    using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                    {
                        // Eingabeparameter erzeugen
                        command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

                        try
                        {
                            // Verbindung öffnen
                            connection.Open();

                            // SQL-Abfrag ausführen
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                // Wenn Abfrageergebnisse vorhanden sind ...
                                if (reader.Read())
                                {
                                    // Sitzungsschlüssel lesen
                                    Guid sqlSessionID = reader.GetGuid(0);

                                    // Sitzung vom SQL Server lesen
                                    ServerSession session = new ServerSession(sqlSessionID,
                                                                              reader.GetDateTime(1),
                                                                              new GenericIdentity(reader.GetString(2)),
                                                                              new SessionVariableAdapter(this,sqlSessionID));

                                    // Transaktion abschließen
                                    scope.Complete();

                                    // Sitzung zurückgeben
                                    return session;
                                }
                                // Transaktion abschließen
                                scope.Complete();
                                
                                // Nichts zurückgeben
                                return null;
                            }
                            
                        }
                        finally
                        {
                            // Verbindung schließen
                            connection.Close();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ruft den Wert einer bestimmten Sitzungsvariable aus der SQL Server Datenbank ab.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <param name="variableName">Variablenname</param>
        /// <returns>Wert der Variable</returns>
        private object GetVariableFromSqlServer(Guid sessionID, string variableName)
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
            {
                // Verbindung zum SQL Server herstellen
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // SQL-Abfrage zusammenstellen
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.Append("SELECT VariableValue ");
                    sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlVariablesTableName);
                    sqlBuilder.Append("WHERE SessionID=@sessionID AND VariableName=@variableName");

                    // SQL-Befehl erzeugen
                    using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                    {
                        // Eingabeparameter erzeugen
                        command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;
                        command.Parameters.Add("@variableName", SqlDbType.NVarChar,255).Value = variableName;

                        try
                        {
                            // Verbindung öffnen
                            connection.Open();

                            // SQL-Abfrag ausführen
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                // Wenn Abfrageergebnisse vorhanden sind ...
                                if (reader.Read())
                                {
                                    // Rohdaten lesen
                                    SqlBytes raw = reader.GetSqlBytes(0);
                                    
                                    // Transaktion abschließen
                                    scope.Complete();

                                    // Wenn kein Wert hinterlegt ist ...
                                    if (raw.IsNull)
                                        // Nichts zurückgeben
                                        return null;

                                    // Binären-Serialisierer erzeugen
                                    BinaryFormatter formatter = new BinaryFormatter();
                                    
                                    // Speicherdatenstrom erzeugen
                                    MemoryStream stream=new MemoryStream(raw.Value);

                                    // Wert aus Rohdaten deserialisieren
                                    object managedValue = formatter.Deserialize(stream);

                                    // Speicherdatenstrom schließen
                                    stream.Close();

                                    // Wert zurückgeben
                                    return managedValue;
                                }
                                // Transaktion abschließen
                                scope.Complete();

                                // Nichts zurückgeben
                                return null;
                            }

                        }
                        finally
                        {
                            // Verbindung schließen
                            connection.Close();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Legt den Wert einer bestimmten Sitzungsvariable in der SQL Server Datenbank fest.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <param name="variableName">Variablenname</param>
        /// <param name="variableValue">Wert der Variable</param>        
        private void SetVariableOnSqlServer(Guid sessionID, string variableName, object variableValue)
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
            {
                // Verbindung zum SQL Server herstellen
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // SQL-Abfrage zusammenstellen
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] ", _sqlSchema, _sqlVariablesTableName);
                    sqlBuilder.Append("WHERE SessionID=@sessionID AND VariableName=@variableName; ");                    
                    sqlBuilder.AppendFormat("INSERT [{0}].[{1}] (SessionID,VariableName,VariableValue) ", _sqlSchema, _sqlVariablesTableName);
                    sqlBuilder.Append("VALUES (@sessionID,@variableName,@variableValue) ");
                    
                    // SQL-Befehl erzeugen
                    using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                    {
                        // Eingabeparameter erzeugen
                        command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;
                        command.Parameters.Add("@variableName", SqlDbType.NVarChar, 255).Value = variableName;

                        // Wenn kein Variablenwert angegeben wurde ...
                        if (variableValue == null)
                            // Leeren Wert als Parameter anfügen
                            command.Parameters.Add("@variableValue", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                        else
                        {
                            // Binären-Serialisierer erzeugen
                            BinaryFormatter formatter = new BinaryFormatter();

                            // Speicherdatenstrom erzeugen
                            MemoryStream stream = new MemoryStream();

                            // Wert serialisieren
                            formatter.Serialize(stream, variableValue);

                            // Wert als Parameter anfügen
                            command.Parameters.Add("@variableValue", SqlDbType.VarBinary, -1).Value = stream.ToArray();

                            // Speicherdatenstrom schließen
                            stream.Close();
                        }
                        try
                        {
                            // Verbindung öffnen
                            connection.Open();

                            // Befehl ausführen
                            command.ExecuteNonQuery();
                        }                        
                        finally
                        {
                            // Verbindung schließen
                            connection.Close();
                        }
                    }
                }
                // Transaktion abschließen
                scope.Complete();
            }
        }

        /// <summary>
        /// Gibt zurück, ob eine bestimmte Sitzung in der SQL Server Datenbank existiert, oder nicht.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <returns>Wahr, wenn die Sitzung existiert, ansonsten Falsch</returns>
        private bool ExistSessionOnSqlServer(Guid sessionID)
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
            {
                // Verbindung zum SQL Server herstellen
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // SQL-Abfrage zusammenstellen
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.Append("SELECT @sessionCount=COUNT(SessionID)");
                    sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlSessionTableName);
                    sqlBuilder.Append("WHERE SessionID=@sessionID");

                    // SQL-Befehl erzeugen
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

                        // Transaktion abschließen
                        scope.Complete();

                        // Ergebnis zurückgeben
                        return ((int)sessionCountParam.Value) == 1;
                    }
                }
            }
        }

        /// <summary>
        /// Speichert eine Sitzung in der SQL Server Datenbank ab.
        /// </summary>
        /// <param name="session">Sitzungsobjekt</param>
        private void StoreSessionOnSqlServer(ServerSession session)
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
            {
                // Wenn keine Sitzung angegeben wurde ...
                if (session == null)
                    // Ausnahme werfen
                    throw new ArgumentNullException("session");

                // Verbindung zum SQL Server herstellen
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // SQL-Stapel zum Löschen und Neuanlegen der Sitzung zusammenstellen
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] WHERE SessionID=@sessionID; ", _sqlSchema, _sqlSessionTableName);
                    sqlBuilder.AppendFormat("INSERT [{0}].[{1}] (SessionID,SessionTimestamp,IdentityName) ", _sqlSchema, _sqlSessionTableName);
                    sqlBuilder.Append("VALUES (@sessionID,@sessionTimestamp,@identityName)");

                    // SQL-Befehl erzeugen
                    using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                    {
                        // Eingabeparameter erzeugen
                        command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = session.SessionID;
                        command.Parameters.Add("@sessionTimestamp", SqlDbType.DateTime).Value = session.Timestamp;
                        command.Parameters.Add("@identityName", SqlDbType.NVarChar, 255).Value = session.Identity.Name;

                        // Sitzung speichern
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();

                        // Transaktion abschließen
                        scope.Complete();
                    }
                }
            }
        }

        /// <summary>
        /// Löscht eine bestimmte Sitzung aus der SQL Server Datenbank.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        private void RemoveSessionFromSqlServer(Guid sessionID)        
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
            {
                // Verbindung zum SQL Server herstellen
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // SQL-DELETE-Anweisung 
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] WHERE SessionID=@sessionID", _sqlSchema, _sqlSessionTableName);

                    using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                    {
                        // Eingabeparameter erzeugen
                        command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

                        // Sitzung speichern
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();

                        // Transaktion abschließen
                        scope.Complete();
                    }
                }
            }
        }

        /// <summary>
        /// Entfernt abgelaufene Sitzungen aus der SQL Server Datenbank.
        /// </summary>
        private void SweepExpiredSessionsFromSqlServer()
        {
            // Transaktion erzwingen
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
            {
                // Verbindung zum SQL Server herstellen
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // SQL-Löschanweisung zusammenstellen
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] ", _sqlSchema, _sqlSessionTableName);
                    sqlBuilder.Append("WHERE DATEDIFF(minute,SessionTimestamp,GETDATE())>@sessionAgeLimit");

                    // SQL-Befehl erzeugen
                    using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
                    {
                        // Eingabeparameter erzeugen
                        command.Parameters.Add("@sessionAgeLimit", SqlDbType.Int).Value = _sessionAgeLimit;

                        // Abgelaufene Sitzungen entfernen
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();

                        // Transaktion abschließen
                        scope.Complete();
                    }
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

        #region Sitzungsvariablen

        /// <summary>
        /// Legt den Wert einer Sitzungsvariablen fest.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        /// <param name="name">Variablenname</param>
        /// <param name="value">Wert</param>
        public void SetSessionVariable(Guid sessionID, string name, object value)
        { 
            // Wenn die angegebene Sitzung existiert ...
            if (ExistSession(sessionID))
                // Variablenwert setzen
                SetVariableOnSqlServer(sessionID, name, value);
        }

        /// <summary>
        /// Gibt den Wert einer Sitzungsvariablen zurück.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        /// <param name="name">Variablenname</param>
        /// <returns>Wert</returns>
        public object GetSessionVariable(Guid sessionID, string name)
        {
            // Wenn die angegebene Sitzung existiert ...
            if (ExistSession(sessionID))
                // Variable lesen
                return GetVariableFromSqlServer(sessionID, name);

            // Nichts zurückgeben
            return null;
        }

        #endregion
    }
}

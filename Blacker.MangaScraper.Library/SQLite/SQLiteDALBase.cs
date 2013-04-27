using System;
using System.Data;
using System.Data.SQLite;
using System.Data.SqlTypes;
using System.IO;
using Blacker.MangaScraper.Library.Exceptions;
using log4net;

namespace Blacker.MangaScraper.Library.SQLite
{
    abstract class SQLiteDALBase
    {
        protected static readonly ILog _log = LogManager.GetLogger(typeof (SQLiteDALBase));

        private const int DefaultCommandTimeout = 30;

        private string _connectionString;

        private readonly SchemaManager _schemaManager = new SchemaManager();

        /// <summary>
        /// SQLite database connection string
        /// </summary>
        private string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    var connectionStringBuilder = new SQLiteConnectionStringBuilder()
                                                      {
                                                          DataSource = Configuration.LibraryConfiguration.Instance.StoragePath,
                                                          Version = 3,
                                                          //Set page size to NTFS cluster size = 4096 bytes
                                                          PageSize = 4096,
                                                          CacheSize = 10000,
                                                          JournalMode = SQLiteJournalModeEnum.Wal,
                                                          Pooling = true,
                                                          ForeignKeys = true,
                                                          LegacyFormat = false,
                                                          FailIfMissing = false
                                                      };

                    _connectionString = connectionStringBuilder.ToString();
                }

                return _connectionString;
            }
        }

        protected SQLiteConnection GetConnection()
        {
            try
            {
                var dir = Path.GetDirectoryName(Configuration.LibraryConfiguration.Instance.StoragePath);

                if (String.IsNullOrEmpty(dir))
                {
                    throw new InvalidOperationException("Invalid path to the database.");
                }
                
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var connection = new SQLiteConnection(ConnectionString);
                connection.Open();

                _schemaManager.CheckSchema(connection);

                return connection;
            }
            catch (Exception ex)
            {
                _log.Error("Unable to establish proper connection to database.", ex);

                throw new StorageException("Unable to establish proper connection to database.", ex);
            }
        }

        protected SQLiteCommand GetTextCommand(string command)
        {
            return new SQLiteCommand(command)
                       {
                           CommandTimeout = DefaultCommandTimeout
                       };
        }

        protected int ExecuteNonQuery(SQLiteCommand command, SQLiteConnection connection)
        {
            return ExecuteNonQuery(command, connection, null);
        }

        protected int ExecuteNonQuery(SQLiteCommand command, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            if (command == null) 
                throw new ArgumentNullException("command");

            if (connection == null) 
                throw new ArgumentNullException("connection");

            if (transaction != null && transaction.Connection != connection)
                throw new ArgumentException("Transaction cannot be used with the passed connection.");

            command.Connection = connection;

            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            try
            {
                return command.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                _log.Error("Unable to perform query.", ex);

                throw new StorageException("Unable to perform query.", ex);
            }
        }

        protected object ExecuteScalar(SQLiteCommand command, SQLiteConnection connection)
        {
            return ExecuteScalar(command, connection, null);
        }

        protected object ExecuteScalar(SQLiteCommand command, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            if (connection == null)
                throw new ArgumentNullException("connection");

            if (transaction != null && transaction.Connection != connection)
                throw new ArgumentException("Transaction cannot be used with the passed connection.");

            command.Connection = connection;

            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            try
            {
                return command.ExecuteScalar();
            }
            catch (SQLiteException ex)
            {
                _log.Error("Unable to perform query.", ex);

                throw new StorageException("Unable to perform query.", ex);
            }
        }

        protected IDataReader ExecuteDataReader(SQLiteCommand command, SQLiteConnection connection)
        {
            return ExecuteDataReader(command, connection, null);
        }

        protected IDataReader ExecuteDataReader(SQLiteCommand command, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            if (connection == null)
                throw new ArgumentNullException("connection");

            if (transaction != null && transaction.Connection != connection)
                throw new ArgumentException("Transaction cannot be used with the passed connection.");

            command.Connection = connection;

            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            try
            {
                return command.ExecuteReader();
            }
            catch (SQLiteException ex)
            {
                _log.Error("Unable to perform query.", ex);

                throw new StorageException("Unable to perform query.", ex);
            }
        }

        protected DataTable ExecuteDataTable(SQLiteCommand command, SQLiteConnection connection)
        {
            return ExecuteDataTable(command, connection, null);
        }

        protected DataTable ExecuteDataTable(SQLiteCommand command, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            if (connection == null)
                throw new ArgumentNullException("connection");

            if (transaction != null && transaction.Connection != connection)
                throw new ArgumentException("Transaction cannot be used with the passed connection.");

            command.Connection = connection;

            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            try
            {
                var table = new DataTable();

                using (var reader = command.ExecuteReader())
                {
                    table.Load(reader);
                    return table;
                }
            }
            catch (SQLiteException ex)
            {
                _log.Error("Unable to perform query.", ex);

                throw new StorageException("Unable to perform query.", ex);
            }
            catch (Exception ex)
            {
                _log.Error("Unable to build DataTable.", ex);

                throw new StorageException("Unable to build DataTable.", ex);
            }
        }

        protected void CommitTransaction(SQLiteTransaction transaction)
        {
            try
            {
                transaction.Commit();
            }
            catch (Exception ex)
            {
                _log.Error("Unable to commit transaction.", ex);

                throw new StorageException("Unable to commit transaction.", ex);
            }
        }

        protected void RollbackTransaction(SQLiteTransaction transaction)
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception ex)
            {
                _log.Error("Unable to rollback transaction.", ex);

                throw new StorageException("Unable to rollback transaction.", ex);
            }
        }

        protected static DateTime GetDBSafeDateTime(DateTime date)
        {
            return (DateTime) (date < (DateTime) SqlDateTime.MinValue
                                   ? SqlDateTime.MinValue
                                   : (date > (DateTime) SqlDateTime.MaxValue
                                          ? SqlDateTime.MaxValue
                                          : date));
        }
    }
}

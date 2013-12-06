using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using Blacker.MangaScraper.Common.Utils;
using Blacker.MangaScraper.Library.Configuration;
using log4net;
using System.Linq;

namespace Blacker.MangaScraper.Library.SQLite
{
    class SchemaManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (SchemaManager));

        /// <summary>
        /// Lock object used during database initialization/upgrade
        /// </summary>
        private static readonly object _syncRoot = new object();

        /// <summary>
        /// Current schema version.
        /// </summary>
        private const long SchemaVersion = 2;

        private const string Schema = @"
PRAGMA user_version = {0};

CREATE TABLE [Scrapers] (
  [ScraperId] GUID NOT NULL ON CONFLICT FAIL, 
  [Name] VARCHAR(512) NOT NULL, 
  PRIMARY KEY ([ScraperId]));

CREATE TABLE [Mangas] (
  [ScraperId] GUID NOT NULL CONSTRAINT [MangaHasScraper] REFERENCES [Scrapers]([ScraperId]) ON DELETE RESTRICT, 
  [MangaId] VARCHAR(1024) NOT NULL, 
  [MangaName] VARCHAR(1024) NOT NULL, 
  [Url] TEXT, 
  PRIMARY KEY ([ScraperId], [MangaId]));

CREATE TABLE [Chapters] (
  [ScraperId] GUID NOT NULL CONSTRAINT [ChapterReferencesScraper] REFERENCES [Scrapers]([ScraperId]) ON DELETE RESTRICT, 
  [MangaId] VARCHAR(1024) NOT NULL, 
  [ChapterId] VARCHAR(1024) NOT NULL, 
  [ChapterName] VARCHAR(1024) NOT NULL, 
  [Url] TEXT NOT NULL, 
  [Downloaded] DATETIME NOT NULL,
  [Path] TEXT,
  [DownloadFolder] TEXT,
  [FormatProviderId] GUID NOT NULL,
  CONSTRAINT [ChapterReferencesManga] FOREIGN KEY([ScraperId], [MangaId]) REFERENCES [Mangas]([ScraperId], [MangaId]) ON DELETE CASCADE, 
  PRIMARY KEY ([ScraperId], [MangaId], [ChapterId]));
";

        /// <summary>
        /// This method checks whether the database uses latest schema and if it doesn't it will automatically initiate schema upgrade.
        /// </summary>
        /// <param name="connection">Connection to the SQLite database</param>
        public void CheckSchema(SQLiteConnection connection)
        {
            long version = GetSchemaVersion(connection);

            if (version == SchemaVersion) // database uses current schema version bail
            {
                return;
            }

            if (version > SchemaVersion)
            {
                throw new InvalidOperationException(
                    String.Format("Unsupported schema version. Database uses newer schema ({0}) than application ({1}).", version, SchemaVersion));
            }

            lock (_syncRoot)
            {
                // reload the schema version to make sure that the database schema was not already updated
                version = GetSchemaVersion(connection);

                // database has not yet been initialized
                if (version == 0)
                {
                    InitDatabase(connection);

                    // since we have created new database we don't need to execute any migration logic
                    return;
                }

                string backupName = String.Format("backup-v{0}-{1}.sqlite", version, DateTime.Now.ToString("yyyyMMdd-HHmmssFF"));

                if (!BackupDatabase(connection, backupName))
                {
                    _log.Warn("Unable to perform backup. Will continue with upgrade.");
                }

                IDictionary<long, ISchemaMigrator> migrators =
                    ReflectionHelper.GetInstances<ISchemaMigrator>(this.GetType().Assembly, Enumerable.Empty<Type>())
                                    .ToDictionary(sm => sm.FromVersion);

                int loopCount = 0; // this will ensure that we will not end up looping forever
                do
                {
                    ISchemaMigrator migrator;

                    migrators.TryGetValue(version, out migrator);

                    if (migrator != null)
                    {
                        migrator.Migrate(connection);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Migrator for this version of schema not found.");
                        
                        _log.WarnFormat("Migrator from database schema version {0} was not found.", version);
                    }

                    version = GetSchemaVersion(connection);
                } while (version < SchemaVersion && ++loopCount < SchemaVersion);
            }
        }

        private void InitDatabase(SQLiteConnection connection)
        {
            try
            {
                using (var command = new SQLiteCommand(String.Format(Schema, SchemaVersion), connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Unable to initialize database.", ex);
                throw;
            }
        }

        /// <summary>
        /// Get schema version
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <returns>Schema version loaded from user_version pragma</returns>
        private long GetSchemaVersion(SQLiteConnection connection)
        {
            try
            {
                using (var command = new SQLiteCommand(@"PRAGMA user_version;", connection))
                {
                    return (long)command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Unable to load schema version.", ex);
                return 0;
            }
        }

        /// <summary>
        /// Create a backup of database
        /// </summary>
        /// <param name="connection">Connection to database which we want to backup</param>
        /// <param name="backupName">Name of the new database</param>
        /// <returns>true if successful; false otherwise</returns>
        private bool BackupDatabase(SQLiteConnection connection, string backupName)
        {
            try
            {
                string dataSource = Path.Combine(
                    Path.GetDirectoryName(LibraryConfiguration.Instance.StoragePath) ?? String.Empty, backupName);

                var connectionStringBuilder = new SQLiteConnectionStringBuilder()
                                                  {
                                                      DataSource = dataSource,
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

                using (var backupConnection = new SQLiteConnection(connectionStringBuilder.ToString()))
                {
                    backupConnection.Open();

                    connection.BackupDatabase(backupConnection, "main", "main", -1, null, -1);
                }

                return true;
            }
            catch (Exception ex)
            {
                _log.Error("Unable to backup database.", ex);

                return false;
            }
        }
    }
}

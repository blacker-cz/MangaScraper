using System;
using System.Data.SQLite;
using log4net;

namespace Blacker.MangaScraper.Library.SQLite
{
    class SchemaManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (SchemaManager));

        /// <summary>
        /// Current schema version.
        /// </summary>
        private const long SchemaVersion = 1;

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
  [IsZip] BOOL NOT NULL DEFAULT '0',
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

            // database has not yet been initialized
            if (version == 0)
            {
                InitDatabase(connection);
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
    }
}

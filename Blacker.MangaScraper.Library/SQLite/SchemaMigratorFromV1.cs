using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Blacker.MangaScraper.Library.SQLite
{
    class SchemaMigratorFromV1 : ISchemaMigrator
    {
        private static readonly Guid ZipFormatProviderId = Guid.Parse("45445BD5-EBED-4F85-BFE1-0CAC718C2642");

        private static readonly Guid FolderFormatProviderId = Guid.Parse("30E047F6-8568-4A0A-95B1-D1796C7C3F30");

        private const string ChaptersTableDefinition = @"
            CREATE TABLE [ChaptersTemp] (
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
                PRIMARY KEY ([ScraperId], [MangaId], [ChapterId]));";

        public long FromVersion
        {
            get { return 1; }
        }

        public void Migrate(SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                // create temporary table
                using (var cmd = new SQLiteCommand(ChaptersTableDefinition, connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                const string selectQuery = @"SELECT ScraperId, MangaId, ChapterId, ChapterName, Url, Downloaded, Path, IsZip FROM Chapters;";
                const string insertQuery = @"INSERT INTO ChaptersTemp(ScraperId, MangaId, ChapterId, ChapterName, Url, Downloaded, Path, DownloadFolder, FormatProviderId) 
                                                VALUES (@ScraperId, @MangaId, @ChapterId, @ChapterName, @Url, @Downloaded, @Path, @DownloadFolder, @FormatProviderId)";

                using (var selectCommand = new SQLiteCommand(selectQuery, connection, transaction))
                using (var insertCommand = new SQLiteCommand(insertQuery, connection, transaction))
                using (IDataReader reader = selectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string downloadFolder = Path.GetDirectoryName(reader.GetString(6));
                        bool isZip = reader.GetBoolean(7);

                        insertCommand.Parameters.AddWithValue("@ScraperId", reader.GetValue(0));
                        insertCommand.Parameters.AddWithValue("@MangaId", reader.GetValue(1));
                        insertCommand.Parameters.AddWithValue("@ChapterId", reader.GetValue(2));
                        insertCommand.Parameters.AddWithValue("@ChapterName", reader.GetValue(3));
                        insertCommand.Parameters.AddWithValue("@Url", reader.GetValue(4));
                        insertCommand.Parameters.AddWithValue("@Downloaded", reader.GetValue(5));
                        insertCommand.Parameters.AddWithValue("@Path", reader.GetValue(6));
                        insertCommand.Parameters.AddWithValue("@DownloadFolder", downloadFolder);
                        insertCommand.Parameters.AddWithValue("@FormatProviderId", isZip ? ZipFormatProviderId : FolderFormatProviderId);

                        insertCommand.ExecuteNonQuery();
                    }
                }

                const string finalizeQuery = @"DROP TABLE Chapters;
                                               ALTER TABLE ChaptersTemp RENAME TO Chapters;
                                               PRAGMA user_version = 2;";

                using (var cmd = new SQLiteCommand(finalizeQuery, connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }
    }
}

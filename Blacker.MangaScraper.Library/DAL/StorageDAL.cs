using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Utils;
using Blacker.MangaScraper.Library.Models;
using Blacker.MangaScraper.Library.SQLite;

namespace Blacker.MangaScraper.Library.DAL
{
    class StorageDAL : SQLiteDALBase
    {
        private const string BaseSelect = @"
                                    SELECT  ch.ScraperId, 
                                            ch.MangaId, 
                                            ch.ChapterId, 
                                            ch.ChapterName, 
                                            ch.Url as ChapterUrl, 
                                            ch.Downloaded,
                                            ch.Path,
                                            ch.IsZip,
                                            m.MangaName, 
                                            m.Url as MangaUrl
                                    FROM Chapters ch
                                    INNER JOIN Mangas m
                                        ON m.ScraperId = ch.ScraperId AND m.MangaId = ch.MangaId
                                    ";

        private static readonly Cache<Tuple<Guid, string>, MangaRecord> MangaRecordsCache = new Cache<Tuple<Guid, string>, MangaRecord>();

        public DownloadedChapterInfo GetChapterInfo(string id)
        {
            if (String.IsNullOrEmpty(id))
                throw new ArgumentException("Id must not be null or empty.", "id");

            const string sql = BaseSelect + "WHERE ch.ChapterId=@ChapterId";

            using (var connection = GetConnection())
            using (var command = GetTextCommand(sql))
            {
                command.Parameters.AddWithValue("@ChapterId", id);

                var table = ExecuteDataTable(command, connection);

                if (table == null || table.Rows.Count != 1)
                    return null;

                return LoadDownloadInfoFromDataRow(table.Rows[0], LoadChapterFromDataRow(table.Rows[0]));
            }
        }

        public IEnumerable<DownloadedChapterInfo> GetChaptersInfo()
        {
            return GetChaptersInfo(String.Empty, Enumerable.Empty<KeyValuePair<string, object>>());
        }

        public IEnumerable<DownloadedChapterInfo> GetChaptersInfo(DateTime newerThan)
        {
            return GetChaptersInfo("ch.Downloaded > @NewerThan", new Dictionary<string, object>()
                                                                  {
                                                                      {"@NewerThan", GetDBSafeDateTime(newerThan)}
                                                                  });
        }

        public IEnumerable<DownloadedChapterInfo> GetChaptersInfo(string search)
        {
            return GetChaptersInfo("ch.ChapterName GLOB @SearchString OR m.MangaName GLOB @SearchString", new Dictionary<string, object>()
                                                                                                              {
                                                                                                                  {"@SearchString", search}
                                                                                                              });
        }

        private IEnumerable<DownloadedChapterInfo> GetChaptersInfo(string condition, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            string sql = BaseSelect + (String.IsNullOrEmpty(condition) ? String.Empty : "WHERE " + condition);

            using (var connection = GetConnection())
            using (var command = GetTextCommand(sql))
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }

                var table = ExecuteDataTable(command, connection);

                if (table == null)
                    return Enumerable.Empty<DownloadedChapterInfo>();

                return (from DataRow row in table.Rows
                        select LoadDownloadInfoFromDataRow(row, LoadChapterFromDataRow(row))
                       ).ToList();
            }
        }

        public bool StoreChapterInfo(DownloadedChapterInfo chapterInfo)
        {
            if (chapterInfo == null) 
                throw new ArgumentNullException("chapterInfo");

            if (chapterInfo.ChapterRecord == null)
                throw new ArgumentException("Chapter record is invalid.", "chapterInfo");

            if (String.IsNullOrEmpty(chapterInfo.ChapterRecord.ChapterId))
                throw new ArgumentException("Chapter record id is invalid.", "chapterInfo");

            if (chapterInfo.ChapterRecord.MangaRecord == null)
                throw new ArgumentException("Manga record is invalid.", "chapterInfo");

            if (String.IsNullOrEmpty(chapterInfo.ChapterRecord.MangaRecord.MangaId))
                throw new ArgumentException("Manga record id is invalid.", "chapterInfo");

            const string insertMangaSql = @"INSERT OR IGNORE 
                                                INTO Mangas(
                                                        ScraperId, 
                                                        MangaId, 
                                                        MangaName, 
                                                        Url) 
                                                VALUES (
                                                        @ScraperId, 
                                                        @MangaId, 
                                                        @MangaName,
                                                        @Url)";
            
            const string insertChapterSql = @"INSERT OR REPLACE 
                                                INTO Chapters(
                                                        ScraperId, 
                                                        MangaId, 
                                                        ChapterId, 
                                                        ChapterName, 
                                                        Url, 
                                                        Downloaded, 
                                                        Path,
                                                        IsZip) 
                                                VALUES (
                                                        @ScraperId, 
                                                        @MangaId, 
                                                        @ChapterId, 
                                                        @ChapterName, 
                                                        @Url, 
                                                        @Downloaded, 
                                                        @Path,
                                                        @IsZip)";

            int affectedRows;

            using (var connection = GetConnection())
            using (var transaction = connection.BeginTransaction())
            {
                using (var command = GetTextCommand(insertMangaSql))
                {
                    command.Parameters.AddWithValue("@ScraperId", chapterInfo.ChapterRecord.MangaRecord.Scraper);
                    command.Parameters.AddWithValue("@MangaId", chapterInfo.ChapterRecord.MangaRecord.MangaId);
                    command.Parameters.AddWithValue("@MangaName", chapterInfo.ChapterRecord.MangaRecord.MangaName);
                    command.Parameters.AddWithValue("@Url", chapterInfo.ChapterRecord.MangaRecord.Url);

                    ExecuteNonQuery(command, connection, transaction);
                }

                using (var command = GetTextCommand(insertChapterSql))
                {
                    command.Parameters.AddWithValue("@ScraperId", chapterInfo.ChapterRecord.MangaRecord.Scraper);
                    command.Parameters.AddWithValue("@MangaId", chapterInfo.ChapterRecord.MangaRecord.MangaId);
                    command.Parameters.AddWithValue("@ChapterId", chapterInfo.ChapterRecord.ChapterId);
                    command.Parameters.AddWithValue("@ChapterName", chapterInfo.ChapterRecord.ChapterName);
                    command.Parameters.AddWithValue("@Url", chapterInfo.ChapterRecord.Url);
                    command.Parameters.AddWithValue("@Downloaded", GetDBSafeDateTime(chapterInfo.Downloaded));
                    command.Parameters.AddWithValue("@Path", chapterInfo.Path);
                    command.Parameters.AddWithValue("@IsZip", chapterInfo.IsZip);

                    affectedRows = ExecuteNonQuery(command, connection, transaction);
                }

                CommitTransaction(transaction);
            }

            return affectedRows == 1;
        }

        public bool RemoveDownloadInfo(string chapterId)
        {
            if (String.IsNullOrEmpty(chapterId))
                throw new ArgumentException("Chapter id must not be null or empty.", "chapterId");

            const string sql = @"DELETE FROM Chapters WHERE ChapterId=@ChapterId";

            using (var connection = GetConnection())
            using (var command = GetTextCommand(sql))
            {
                command.Parameters.AddWithValue("@ChapterId", chapterId);

                return ExecuteNonQuery(command, connection) > 0;
            }
        }

        public void UpdateScrapers(IEnumerable<IScraper> scrapers)
        {
            if (scrapers == null) 
                throw new ArgumentNullException("scrapers");

            const string sql = @"INSERT OR IGNORE INTO Scrapers (ScraperId, Name) VALUES (@ScraperId, @Name)";

            using (var connection = GetConnection())
            using (var transaction = connection.BeginTransaction())
            {
                using (var command = GetTextCommand(sql))
                {
                    foreach (IScraper scraper in scrapers)
                    {
                        command.Parameters.AddWithValue("@ScraperId", scraper.ScraperGuid);
                        command.Parameters.AddWithValue("@Name", scraper.Name);

                        ExecuteNonQuery(command, connection, transaction);

                        command.Parameters.Clear();
                    }
                }

                CommitTransaction(transaction);
            }
        }

        public IDictionary<string, int> GetRecentFolders(Guid scraperId, string mangaId)
        {
            if (scraperId == Guid.Empty)
                throw new ArgumentException("Scraper Id must not empty.", "scraperId");

            if (String.IsNullOrEmpty(mangaId))
                throw new ArgumentException("Manga Id must not be null or empty", "mangaId");

            // let's select just latest 5 records
            const string sql = @"SELECT Path, IsZip FROM Chapters WHERE ScraperId=@ScraperId AND MangaId=@MangaId ORDER BY Downloaded DESC LIMIT 5";

            var folders = new Dictionary<string, int>();

            using (var connection = GetConnection())
            using (var command = GetTextCommand(sql))
            {
                command.Parameters.AddWithValue("@ScraperId", scraperId);
                command.Parameters.AddWithValue("@MangaId", mangaId);

                var table = ExecuteDataTable(command, connection);

                if (table == null)
                    return folders;

                foreach (DataRow row in table.Rows)
                {
                    bool isFile = Convert.ToBoolean(row["IsZip"]);
                    string downloadPath = Convert.ToString(row["Path"]);

                    string folder = isFile
                                        ? System.IO.Path.GetDirectoryName(downloadPath)
                                        : System.IO.Directory.GetParent(downloadPath).FullName;

                    if(folder == null)
                        continue;

                    if (folders.ContainsKey(folder))
                    {
                        folders[folder]++;
                    }
                    else
                    {
                        folders[folder] = 1;
                    }
                }
            }

            return folders;
        }

        private static ChapterRecord LoadChapterFromDataRow(DataRow row)
        {
            var mangaRecordKey = new Tuple<Guid, string>((Guid) row["ScraperId"], Convert.ToString(row["MangaId"]));
            var mangaRecord = MangaRecordsCache[mangaRecordKey];

            if (mangaRecord == null)
            {
                mangaRecord = new MangaRecord()
                                  {
                                      MangaId = Convert.ToString(row["MangaId"]),
                                      MangaName = Convert.ToString(row["MangaName"]),
                                      Scraper = (Guid) row["ScraperId"],
                                      Url = row["MangaUrl"] as string
                                  };

                MangaRecordsCache[mangaRecordKey] = mangaRecord;
            }

            var chapterRecord = new ChapterRecord()
                                    {
                                        ChapterId = Convert.ToString(row["ChapterId"]),
                                        ChapterName = Convert.ToString(row["ChapterName"]),
                                        Scraper = (Guid) row["ScraperId"],
                                        Url = Convert.ToString(row["ChapterUrl"]),
                                        MangaRecord = mangaRecord
                                    };

            return chapterRecord;
        }

        private static DownloadedChapterInfo LoadDownloadInfoFromDataRow(DataRow row, ChapterRecord chapter)
        {
            return new DownloadedChapterInfo(chapter)
                       {
                           Path = row["Path"] as string,
                           Downloaded = Convert.ToDateTime(row["Downloaded"]),
                           IsZip = Convert.ToBoolean(row["IsZip"])
                       };
        }
    }
}

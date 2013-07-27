using System;
using System.Collections.Generic;
using System.Linq;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Library.DAL;
using Blacker.MangaScraper.Library.Exceptions;
using Blacker.MangaScraper.Library.Models;

namespace Blacker.MangaScraper.Library
{
    public class LibraryManager : ILibraryManager
    {
        private readonly StorageDAL _storage = new StorageDAL();

        public DownloadedChapterInfo GetDownloadInfo(string chapterId)
        {
            if (String.IsNullOrEmpty(chapterId))
                throw new ArgumentException("Chapter id must not be null or empty.", "chapterId");

            return _storage.GetChapterInfo(chapterId);
        }

        public DownloadedChapterInfo GetDownloadInfo(IChapterRecord chapterRecord)
        {
            if (chapterRecord == null)
                throw new ArgumentNullException("chapterRecord");

            if (String.IsNullOrEmpty(chapterRecord.ChapterId))
                throw new ArgumentException("Invalid chapter record, chapter id must not be null or empty.", "chapterRecord");

            return _storage.GetChapterInfo(chapterRecord.ChapterId);
        }

        public bool StoreDownloadInfo(DownloadedChapterInfo downloadedChapterInfo)
        {
            return _storage.StoreChapterInfo(downloadedChapterInfo);
        }

        public bool RemoveDownloadInfo(string chapterId)
        {
            return _storage.RemoveDownloadInfo(chapterId);
        }

        public bool RemoveDownloadInfo(DownloadedChapterInfo downloadedChapterInfo)
        {
            if (downloadedChapterInfo == null)
                throw new ArgumentNullException("downloadedChapterInfo");

            if (downloadedChapterInfo.ChapterRecord == null)
                throw new ArgumentException("Invalid chapter record.", "downloadedChapterInfo");

            return _storage.RemoveDownloadInfo(downloadedChapterInfo.ChapterRecord.ChapterId);
        }

        public IEnumerable<DownloadedChapterInfo> GetDownloads()
        {
            return _storage.GetChaptersInfo();
        }

        public IEnumerable<DownloadedChapterInfo> GetDownloads(DateTime newerThen)
        {
            return _storage.GetChaptersInfo(newerThen);
        }

        public IEnumerable<DownloadedChapterInfo> GetDownloads(string search)
        {
            if (String.IsNullOrEmpty(search))
                throw new ArgumentException("Search string must not be null or emtpy.");

            return _storage.GetChaptersInfo(search);
        }

        public string GetRecentOutputFolder(IMangaRecord mangaRecord)
        {
            if (mangaRecord == null) 
                throw new ArgumentNullException("mangaRecord");

            try
            {
                IDictionary<string, int> folders = _storage.GetRecentFolders(mangaRecord.Scraper, mangaRecord.MangaId);

                if (folders.Count == 0)
                    return null;

                // return the folder name with most usages
                return folders.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }
            catch (StorageException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IEnumerable<IMangaRecord> GetRecentlyDownloadedMangas(DateTime downloadedAfter)
        {
            return _storage.GetRecentlyDownloadedMangas(downloadedAfter);
        }

        public void UpdateScrapersList(IEnumerable<IScraper> existingScrapers)
        {
            if (existingScrapers != null)
            {
                _storage.UpdateScrapers(existingScrapers);
            }
        }
    }
}
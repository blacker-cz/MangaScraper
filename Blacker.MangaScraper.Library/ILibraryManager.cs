using System;
using System.Collections.Generic;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Library.Models;

namespace Blacker.MangaScraper.Library
{
    public interface ILibraryManager
    {
        DownloadedChapterInfo GetDownloadInfo(string chapterId);
        DownloadedChapterInfo GetDownloadInfo(IChapterRecord chapterRecord);
        bool StoreDownloadInfo(DownloadedChapterInfo downloadedChapterInfo);
        bool RemoveDownloadInfo(string chapterId);
        bool RemoveDownloadInfo(DownloadedChapterInfo downloadedChapterInfo);
        IEnumerable<DownloadedChapterInfo> GetDownloads();
        IEnumerable<DownloadedChapterInfo> GetDownloads(DateTime newerThen);
        IEnumerable<DownloadedChapterInfo> GetDownloads(string search);
        string GetRecentOutputFolder(IMangaRecord mangaRecord);
        void UpdateScrapersList(IEnumerable<IScraper> existingScrapers);
    }
}
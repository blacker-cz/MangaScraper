using System;
using System.Collections.Generic;
using Blacker.Scraper.Models;
using System.IO;
using Blacker.Scraper.Events;

namespace Blacker.Scraper
{
    public interface IScraper : IDownloadProgressReporter
    {
        /// <summary>
        /// Get scraper name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get available chapters for given manga.
        /// </summary>
        /// <param name="manga">Manga</param>
        /// <returns>List of available chapters</returns>
        IEnumerable<ChapterRecord> GetAvailableChapters(MangaRecord manga);

        /// <summary>
        /// Get list of available mangas filtered by name (or its part)
        /// </summary>
        /// <param name="filter">Part of manga name (ignores case and diacritics)</param>
        /// <returns>List of available mangas</returns>
        IEnumerable<MangaRecord> GetAvailableMangas(string filter);

        /// <summary>
        /// Download chapter to file compressed using ZIP algorithm
        /// </summary>
        /// <param name="chapter">Chapter info</param>
        /// <param name="file">Output file info</param>
        void DownloadChapter(ChapterRecord chapter, FileInfo file);

        /// <summary>
        /// Download chapter to directory
        /// </summary>
        /// <param name="chapter">Chapter info</param>
        /// <param name="directory">Output directory info</param>
        /// <param name="createDir">Flag if directory should be created if it does not exist (optional, defaul true)</param>
        void DownloadChapter(ChapterRecord chapter, DirectoryInfo directory, bool createDir = true);
    }
}

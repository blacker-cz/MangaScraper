using System.Collections.Generic;
using Blacker.Scraper.Models;

namespace Blacker.Scraper
{
    public interface IScraper : IDownloadProvider, IDownloadProgressReporter
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
    }
}

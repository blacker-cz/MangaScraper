using System;
using System.Collections.Generic;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.MangaScraper.Common
{
    /// <summary>
    /// IScraper interface.
    /// Implementation of this interface must be thread safe
    /// </summary>
    public interface IScraper
    {
        /// <summary>
        /// Get scraper name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Scraper unique guid
        /// </summary>
        Guid ScraperGuid { get; }

        /// <summary>
        /// Get available chapters for given manga.
        /// </summary>
        /// <param name="manga">Manga</param>
        /// <returns>List of available chapters</returns>
        IEnumerable<IChapterRecord> GetAvailableChapters(IMangaRecord manga);

        /// <summary>
        /// Get list of available mangas filtered by name (or its part)
        /// </summary>
        /// <param name="filter">Part of manga name (ignores case and diacritics)</param>
        /// <returns>List of available mangas</returns>
        IEnumerable<IMangaRecord> GetAvailableMangas(string filter);

        /// <summary>
        /// Method to get downloader which is already setup for this scraper
        /// </summary>
        /// <returns>Returns new instance of IDownloader implementation</returns>
        IDownloader GetDownloader();
    }
}

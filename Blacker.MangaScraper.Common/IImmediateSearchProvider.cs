using System.Collections.Generic;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.MangaScraper.Common
{
    /// <summary>
    /// Immediate search provider.
    /// Classes implementing this interface can provide results for available mangas immediately.
    /// </summary>
    public interface IImmediateSearchProvider
    {
        /// <summary>
        /// Get list of available mangas filtered by name (or its part).
        /// Immediate search (called when user starts typing to the search field).
        /// </summary>
        /// <param name="filter">Part of manga name (ignores case and diacritics)</param>
        /// <returns>List of available mangas</returns>
        IEnumerable<IMangaRecord> GetAvailableMangasImmediate(string filter);
    }
}

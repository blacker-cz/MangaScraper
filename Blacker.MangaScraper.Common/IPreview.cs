using System.Collections.Generic;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.MangaScraper.Common
{
    public interface IPreview
    {
        /// <summary>
        /// Preview of available mangas that will show when scraper is selected.
        /// </summary>
        /// <returns>List of manga records</returns>
        IEnumerable<IMangaRecord> Preview();
    }
}

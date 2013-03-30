using System;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.Scraper.Models
{
    internal class MangaRecord : IMangaRecord
    {
        public MangaRecord(Guid scraper, string mangaId)
        {
            if (String.IsNullOrEmpty(mangaId))
                throw new ArgumentException("Manga identifier cannot be null or empty.", "mangaId");

            MangaId = mangaId;
            Scraper = scraper;
        }

        public string MangaId { get; private set; }

        public string MangaName { get; set; }

        public string Url { get; set; }

        public Guid Scraper { get; private set; }
    }
}

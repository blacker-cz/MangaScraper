using System;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.Scraper.Models
{
    internal class MangaRecord : IMangaRecord
    {
        public MangaRecord(Guid scraper)
        {
            Scraper = scraper;
        }

        public string MangaName { get; set; }

        public string Url { get; set; }

        public Guid Scraper { get; private set; }
    }
}

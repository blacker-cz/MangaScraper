using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Models
{
    public class MangaRecord
    {
        public MangaRecord(Scrapers scraper)
        {
            Scraper = scraper;
        }

        public string MangaName { get; set; }

        public string Url { get; set; }

        public Scrapers Scraper { get; private set; }
    }
}

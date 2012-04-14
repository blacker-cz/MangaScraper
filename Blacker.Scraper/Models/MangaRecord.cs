using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Models
{
    public class MangaRecord
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

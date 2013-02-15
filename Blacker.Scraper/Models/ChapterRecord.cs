using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Models
{
    public class ChapterRecord
    {
        public ChapterRecord(Guid scraper)
        {
            Scraper = scraper;
        }

        public string MangaName 
        {
            get { return MangaRecord != null ? MangaRecord.MangaName : "n/a"; }
        }

        public string ChapterName { get; set; }

        public string Url { get; set; }

        public MangaRecord MangaRecord { get; set; }

        public Guid Scraper { get; private set; }
    }
}

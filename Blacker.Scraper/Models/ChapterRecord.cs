using System;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.Scraper.Models
{
    internal class ChapterRecord : IChapterRecord
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

        public IMangaRecord MangaRecord { get; set; }

        public Guid Scraper { get; private set; }
    }
}

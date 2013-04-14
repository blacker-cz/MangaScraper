using System;
using System.Data;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.MangaScraper.Library.Models
{
    internal class ChapterRecord : IChapterRecord
    {
        public string ChapterId { get; set; }

        public string MangaName 
        {
            get { return MangaRecord != null ? MangaRecord.MangaName : "n/a"; }
        }

        public string ChapterName { get; set; }

        public string Url { get; set; }

        public IMangaRecord MangaRecord { get; set; }

        public Guid Scraper { get; set; }
    }
}

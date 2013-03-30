using System;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.Scraper.Models
{
    internal class ChapterRecord : IChapterRecord
    {
        public ChapterRecord(Guid scraper, string chapterId)
        {
            if (String.IsNullOrEmpty(chapterId))
                throw new ArgumentException("Chapter identifier cannot be null or empty.", "chapterId");

            ChapterId = chapterId;
            Scraper = scraper;
        }

        public string ChapterId { get; private set; }

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

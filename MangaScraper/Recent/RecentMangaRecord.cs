using System;
using System.Linq;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.MangaScraper.Recent
{
    internal class RecentMangaRecord : IMangaRecord
    {
        private readonly IMangaRecord _mangaRecord;
        private readonly IScraper _scraper;

        public RecentMangaRecord(IMangaRecord mangaRecord)
        {
            if (mangaRecord == null) 
                throw new ArgumentNullException("mangaRecord");

            _mangaRecord = mangaRecord;

            _scraper = ScraperLoader.Instance.AllScrapers.FirstOrDefault(s => s.ScraperGuid == _mangaRecord.Scraper);
        }

        public string MangaId
        {
            get { return _mangaRecord.MangaId; }
        }

        public string MangaName
        {
            get { return _mangaRecord.MangaName; }
        }

        public string Url
        {
            get { return _mangaRecord.Url; }
        }

        public Guid Scraper
        {
            get { return _mangaRecord.Scraper; }
        }

        public IScraper ScraperInstance
        {
            get { return _scraper; }
        }

        public string ScraperName
        {
            get { return _scraper != null ? _scraper.Name : String.Empty; }
        }
    }
}

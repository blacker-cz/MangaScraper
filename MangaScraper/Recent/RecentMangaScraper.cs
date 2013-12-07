using System;
using System.Collections.Generic;
using System.Linq;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Library;
using Blacker.MangaScraper.Services;

namespace Blacker.MangaScraper.Recent
{
    internal class RecentMangaScraper : IScraper, IImmediateSearchProvider, IPreview
    {
        private static readonly object _syncRoot = new object();

        private WeakReference _mangaRecords;

        public string Name
        {
            get { return "Recently Downloaded"; }
        }

        public Guid ScraperGuid
        {
            get { return Guid.Parse("8AC18E08-C59E-4D67-99A8-FECEFC250851"); }
        }

        public IEnumerable<IChapterRecord> GetAvailableChapters(IMangaRecord manga)
        {
            if (manga == null) 
                throw new ArgumentNullException("manga");

            var scraper = ScraperLoader.Instance.AllScrapers.FirstOrDefault(s => s.ScraperGuid == manga.Scraper);

            if (scraper == null)
                return Enumerable.Empty<IChapterRecord>();

            return scraper.GetAvailableChapters(manga);
        }

        public IEnumerable<IMangaRecord> GetAvailableMangas(string filter)
        {
            return GetMangas(false).Where(mr => mr.MangaName.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1).ToList();
        }

        public IDownloader GetDownloader()
        {
            // this should not get called as this scraper is basically used to redirect to other scrapers
            throw new NotSupportedException();
        }

        public IEnumerable<IMangaRecord> GetAvailableMangasImmediate(string filter)
        {
            return GetAvailableMangas(filter);
        }

        public IEnumerable<IMangaRecord> Preview()
        {
            return GetMangas(true);
        }

        private IEnumerable<RecentMangaRecord> GetMangas(bool force)
        {
            IEnumerable<RecentMangaRecord> mangas;

            lock (_syncRoot)
            {
                if (force || _mangaRecords == null || ((mangas = _mangaRecords.Target as IEnumerable<RecentMangaRecord>) == null))
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-Properties.Settings.Default.RecentMangaDaysNum);

                    mangas = ServiceLocator.Instance.GetService<ILibraryManager>()
                                           .GetRecentlyDownloadedMangas(cutoffDate)
                                           .Select(mr => new RecentMangaRecord(mr))
                                           .Where(rmr => rmr.ScraperInstance != null)
                                           .ToList();

                    _mangaRecords = new WeakReference(mangas);
                }
            }

            return mangas;
        }
    }
}

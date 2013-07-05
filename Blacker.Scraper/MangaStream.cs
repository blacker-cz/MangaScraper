using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Common.Utils;
using Blacker.Scraper.Models;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;
using log4net;

namespace Blacker.Scraper
{
    public sealed class MangaStream : BaseScraper, IScraper, IImmediateSearchProvider, IPreload
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MangaStream));

        private const string DictionaryUrl = "http://mangastream.com/manga";
        private const string MangaStreamUrl = "http://mangastream.com";

        private readonly Cache<string, object> _cache;

        private const string MangasCacheKey = "Mangas";
        private const string ChapterCacheKey = "Chapters";

        private readonly object _syncRoot = new object();

        public MangaStream()
        {
            _cache = new Cache<string, object>();
        }

        protected override string BaseUrl
        {
            get { return MangaStreamUrl; }
        }

        #region IScraper implementation

        public string Name { get { return "MangaStream"; } }

        public Guid ScraperGuid
        {
            get { return Guid.Parse("9a2e1bcb-923f-45d1-849a-2e341427a58b"); }
        }

        public IEnumerable<IChapterRecord> GetAvailableChapters(IMangaRecord manga)
        {
            if (manga == null)
                throw new ArgumentNullException("manga");
            if (manga.Scraper != ScraperGuid)
                throw new ArgumentException("Manga record is not for " + Name, "manga");

            var cacheKey = ChapterCacheKey + manga.MangaName + manga.Url;

            var cached = _cache[cacheKey] as IEnumerable<ChapterRecord>;
            if (cached != null) // if chapters are already in cache return them
                return cached;

            var records = new List<ChapterRecord>();
            var document = WebHelper.GetHtmlDocument(manga.Url);

            var chapterAnchors = document.SelectNodes(@"//div[contains(@class, ""main-body"")]//table/tr/td/a");
            if (chapterAnchors == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var chapterAnchor in chapterAnchors)
            {
                var url = GetFullUrl(chapterAnchor.Attributes["href"].Value);

                records.Add(new ChapterRecord(ScraperGuid, url)
                {
                    ChapterName = CleanupText(chapterAnchor.InnerText),
                    Url = url,
                    MangaRecord = manga
                });
            }

            // save to cache
            _cache[cacheKey] = records;

            return records;
        }

        public IEnumerable<IMangaRecord> GetAvailableMangas(string filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            return Mangas.Where(mr => mr.MangaName.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        public IDownloader GetDownloader()
        {
            return new Downloader(GetPages, @"//img[@id=""manga-page""]");
        }

        #endregion // IScraper implementation

        #region IImmediateSearchProvider implementation

        public IEnumerable<IMangaRecord> GetAvailableMangasImmediate(string filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            return Mangas.Where(mr => mr.MangaName.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        #endregion // IImediateSearchProvider implementation

        #region IPreload implementation

        public void PreloadDirectory()
        {
            lock (_syncRoot)
            {
                // load mangas to cache
                var mangas = _cache[MangasCacheKey] as IEnumerable<MangaRecord>;
                if (mangas == null)
                    _cache[MangasCacheKey] = LoadAllMangas();
            }
        }

        #endregion // IPreload implementation

        #region Private properties

        private IEnumerable<MangaRecord> Mangas
        {
            get
            {
                lock (_syncRoot)
                {
                    var mangas = _cache[MangasCacheKey] as IEnumerable<MangaRecord>;
                    if (mangas == null)
                        _cache[MangasCacheKey] = LoadAllMangas();

                    return _cache[MangasCacheKey] as IEnumerable<MangaRecord>;
                }
            }
        }

        #endregion // Private properties

        #region Private methods

        private IEnumerable<MangaRecord> LoadAllMangas()
        {
            var records = new List<MangaRecord>();
            var document = WebHelper.GetHtmlDocument(DictionaryUrl);

            var mangaAnchors = document.SelectNodes(@"//div[contains(@class, ""main-body"")]//table/tr/td/strong/a");
            if (mangaAnchors == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var mangaAnchor in mangaAnchors)
            {
                if (string.IsNullOrEmpty(mangaAnchor.InnerText))
                    continue;

                var mangaName = CleanupText(mangaAnchor.InnerText);
                var url = GetFullUrl(mangaAnchor.Attributes["href"].Value);

                records.Add(new MangaRecord(ScraperGuid, url)
                {
                    MangaName = CleanupText(mangaName),
                    Url = url
                });
            }

            return records;
        }

        private IDictionary<int, string> GetPages(IChapterRecord chapter)
        {
            IDictionary<int, string> pages = new Dictionary<int, string>();

            var document = WebHelper.GetHtmlDocument(chapter.Url);

            GetPagesRecursive(document, pages);

            return pages;
        }

        private void GetPagesRecursive(HtmlAgilityPack.HtmlNode document, IDictionary<int, string> pages)
        {
            var chapterPages = document.SelectNodes(@"//div[@class=""main-body""]//div[@class=""btn-group""][2]/ul[@class=""dropdown-menu""]/li/a");
            if (chapterPages == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            int addedCount = 0;

            foreach (var pageLink in chapterPages)
            {
                int pageNumber = 0;
                var url = GetFullUrl(pageLink.Attributes["href"].Value);

                if (pages.Any(kvp => kvp.Value == url)) // skip duplicate urls
                    continue;

                if (!Int32.TryParse(Regex.Match(pageLink.InnerText, @"\d+").Value, out pageNumber))
                    _log.Error("Unable to parse page number '" + pageLink.InnerText + "'");

                if (pages.ContainsKey(pageNumber))  // if page is already in dictionary use random number instead
                    pageNumber = Random;

                pages.Add(pageNumber, url);
                addedCount++;
            }

            if (addedCount > 0)
            {
                var pageRecord = pages.OrderByDescending(kvp => kvp.Key).Skip(1).FirstOrDefault();
                if (pageRecord.Equals(default(KeyValuePair<int, string>)))
                    return;

                var nextDocument = WebHelper.GetHtmlDocument(pageRecord.Value);

                GetPagesRecursive(nextDocument, pages);
            }
        }

        #endregion // Private methods
    }
}

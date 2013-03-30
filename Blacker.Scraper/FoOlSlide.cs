using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.Scraper.Models;
using log4net;
using Blacker.Scraper.Cache;
using Blacker.Scraper.Exceptions;
using Blacker.Scraper.Helpers;
using System.Text.RegularExpressions;

namespace Blacker.Scraper
{
    public class FoOlSlide : BaseScraper, IScraper, IImmediateSearchProvider, IPreload, IScraperIgnore
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FoOlSlide));

        private readonly FoOlSlideConfig _configuration;

        private readonly Cache<string, object> _cache;

        private const string MangasCacheKey = "Mangas";
        private const string ChapterCacheKey = "Chapters";

        private readonly object _syncRoot = new object();

        public FoOlSlide(FoOlSlideConfig configuration)
        {
            if (string.IsNullOrEmpty(configuration.Name))
                throw new ArgumentException("Name must not be null or empty.", "configuration");
            if (configuration.ScraperGuid == Guid.Empty)
                throw new ArgumentException("ScraperGuid must not be empty.", "configuration");
            if (string.IsNullOrEmpty(configuration.BaseUrl))
                throw new ArgumentException("BaseUrl must not be null or empty.", "configuration");
            if (string.IsNullOrEmpty(configuration.DirectoryUrl))
                throw new ArgumentException("DirectoryUrl must not be null or empty.", "configuration");

            _configuration = (FoOlSlideConfig)configuration.Clone();

            _cache = new Cache<string, object>();
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("option");
        }

        protected override string BaseUrl
        {
            get { return _configuration.BaseUrl; }
        }

        #region IScraper implementation

        public string Name
        {
            get { return _configuration.Name; }
        }

        public Guid ScraperGuid
        {
            get { return _configuration.ScraperGuid; }
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

            var chapters = document.SelectNodes(@"//div[@class=""list""]/div[@class=""element""]/div[@class=""title""]/a");
            if (chapters == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var chapter in chapters)
            {
                var url = GetFullUrl(chapter.Attributes["href"].Value);

                records.Add(new ChapterRecord(ScraperGuid, url)
                {
                    ChapterName = CleanupText(chapter.InnerText),
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
            return new Downloader(GetPages, @"//div[@id=""page""]//img[@class=""open""]");
        }

        #endregion // IScraper implementation
        
        #region IImmediateSearchProvider implementation

        public IEnumerable<IMangaRecord> GetAvailableMangasImmediate(string filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            return Mangas.Where(mr => mr.MangaName.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        #endregion // IImmediateSearchProvider implementation

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
            var document = WebHelper.GetHtmlDocument(_configuration.DirectoryUrl);

            var mangas = document.SelectNodes(@"//div[contains(@class, ""list"") and contains(@class, ""series"")]/div[@class=""group""]/div[@class=""title""]/a");
            if (mangas == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var manga in mangas)
            {
                if (string.IsNullOrEmpty(manga.InnerText))
                    continue;

                var url = GetFullUrl(manga.Attributes["href"].Value);

                records.Add(new MangaRecord(ScraperGuid, url)
                {
                    MangaName = CleanupText(manga.InnerText),
                    Url = url
                });
            }

            return records;
        }

        private IDictionary<int, string> GetPages(IChapterRecord chapter)
        {
            IDictionary<int, string> pages = new Dictionary<int, string>();

            var document = WebHelper.GetHtmlDocument(chapter.Url);
            var chapterPages = document.SelectNodes(@"(//div[contains(@class, ""tbtitle"") and contains(@class, ""dropdown_right"")]/ul[@class=""dropdown""])/li/a");
            if (chapterPages == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var pageLink in chapterPages)
            {
                int pageNumber = 0;

                if (!Int32.TryParse(Regex.Match(pageLink.InnerText, @"\d+").Value, out pageNumber))
                    _log.Error("Unable to parse page number '" + pageLink.InnerText + "'");

                if (pages.ContainsKey(pageNumber))  // if page is already in dictionary use random number instead
                    pageNumber = Random;

                pages.Add(pageNumber, GetFullUrl(pageLink.Attributes["href"].Value));
            }

            return pages;
        }

        #endregion // Private methods

        public class FoOlSlideConfig : ICloneable
        {
            public string Name { get; set; }
            
            public Guid ScraperGuid { get; set; }

            public string DirectoryUrl { get; set; }

            public string BaseUrl { get; set; }

            #region ICloneable implementation

            public object Clone()
            {
                return MemberwiseClone();
            }

            #endregion // ICloneable implementation
        }
    }
}

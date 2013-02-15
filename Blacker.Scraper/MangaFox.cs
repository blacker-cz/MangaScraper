using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using Blacker.Scraper.Cache;
using Blacker.Scraper.Models;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;

namespace Blacker.Scraper
{
    public sealed class MangaFox : BaseScraper, IScraper, IImmediateSearchProvider, IPreload
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MangaFox));

        private const string DictionaryUrl = "http://mangafox.me/manga";
        private const string MangaFoxUrl = "http://mangafox.me";

        private Cache<string, object> _cache;

        private const string MangasCacheKey = "Mangas";
        private const string ChapterCacheKey = "Chapters";

        private readonly object _syncRoot = new object();

        public MangaFox()
        {
            _cache = new Cache<string, object>();
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("option");
        }

        protected override string BaseUrl
        {
            get { return MangaFoxUrl; }
        }

        #region IScraper implementation

        public string Name { get { return "MangaFox.me"; } }

        public Guid ScraperGuid
        {
            get { return Guid.Parse("bbc1ed62-3535-4131-9c00-5148dd2f2035"); }
        }

        public IEnumerable<ChapterRecord> GetAvailableChapters(MangaRecord manga)
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

            var chapters = document.SelectNodes(@"//ul[@class=""chlist""]/li//h3|//ul[@class=""chlist""]/li//h4");
            if (chapters == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var chapter in chapters)
            {
                records.Add(new ChapterRecord(ScraperGuid)
                {
                    ChapterName = CleanupText(chapter.InnerText),
                    Url = GetFullUrl(chapter.ChildNodes.FirstOrDefault(n => n.Name == "a").Attributes["href"].Value),
                    MangaRecord = manga
                });
            }

            // save to cache
            _cache[cacheKey] = records;

            return records;
        }

        public IEnumerable<MangaRecord> GetAvailableMangas(string filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            return Mangas.Where(mr => mr.MangaName.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        public IDownloader GetDownloader()
        {
            return new Downloader(GetPages, @"//img[@id=""image""]");
        }

        #endregion // IScraper implementation

        #region IImmediateSearchProvider implementation

        public IEnumerable<MangaRecord> GetAvailableMangasImmediate(string filter)
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

            var mangas = document.SelectNodes(@"//div[@class=""manga_list""]//ul/li/a");
            if (mangas == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var manga in mangas)
            {
                if (string.IsNullOrEmpty(manga.InnerText))
                    continue;

                records.Add(new MangaRecord(ScraperGuid)
                {
                    MangaName = CleanupText(manga.InnerText),
                    Url = GetFullUrl(manga.Attributes["href"].Value)
                });
            }

            return records;
        }

        private IDictionary<int, string> GetPages(ChapterRecord chapter)
        {
            IDictionary<int, string> pages = new Dictionary<int, string>();

            var document = WebHelper.GetHtmlDocument(chapter.Url);

            var commentsLink = document.SelectSingleNode(@"//a[@id=""comments""]");
            if (commentsLink == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            var link = commentsLink.Attributes["href"].Value;

            var chapterPages = document.SelectNodes(@"//select[@class=""m""][1]/option");
            if (chapterPages == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var pageLink in chapterPages)
            {
                int pageNumber = 0;

                if (!Int32.TryParse(pageLink.InnerText, out pageNumber))
                    continue;

                if (pages.ContainsKey(pageNumber)) // if page is already in dictionary then skip adding it
                    continue;

                pages.Add(pageNumber, link + pageNumber + ".html");
            }

            return pages;
        }

        #endregion // Private methods
    }
}

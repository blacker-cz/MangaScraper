using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Blacker.Scraper.Models;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;
using Blacker.Scraper.Cache;
using System.Text.RegularExpressions;

namespace Blacker.Scraper
{
    public class EGScans : BaseScraper, IScraper, IImmediateSearchProvider, IPreload
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EGScans));

        private const string EGScansUrl = "http://readonline.egscans.com/";
        private const string EGScansMangaUrlFormat = EGScansUrl + "/{0}";   // {0} - value of manga option from manga select list
        private const string EGScansChapterUrlFormat = "{0}/{1}";  // {0} - manga url, {1} - value of the chapter from chapter select list

        private Cache<string, object> _cache;

        private const string MangasCacheKey = "Mangas";
        private const string ChapterCacheKey = "Chapters";

        // regular expression used to extract image url from page
        private readonly Regex _pagesRegex = new Regex(@"img_url\.push\(\s*'([^']+)'\s*\)\s*;");

        private readonly object _syncRoot = new object();

        public EGScans()
        {
            _cache = new Cache<string, object>();
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("option");
        }

        protected override string BaseUrl
        {
            get { return EGScansUrl; }
        }

        #region IScraper implementation

        public string Name
        {
            get { return "EGScans.com"; }
        }

        public Guid ScraperGuid
        {
            get { return Guid.Parse("1f8913bc-dc18-4021-a2e6-41b6df27f2c4"); }
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

            var chapters = document.SelectNodes(@"//select[@name=""chapter""]/option");
            if (chapters == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var chapter in chapters)
            {
                records.Add(new ChapterRecord(ScraperGuid)
                {
                    ChapterName = CleanupText(chapter.InnerText),
                    Url = String.Format(EGScansChapterUrlFormat, manga.Url, chapter.Attributes["value"].Value),
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
            // the list of pages is actually list of images so we will only return url when asked for image (this will also save us a few requests)
            return new Downloader(GetPages, x => x);
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
            var document = WebHelper.GetHtmlDocument(EGScansUrl);

            var mangas = document.SelectNodes(@"//select[@name=""manga""]/option[@value!=""0""]");
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
                    Url = String.Format(EGScansMangaUrlFormat, manga.Attributes["value"].Value)
                });
            }

            return records;
        }

        private IDictionary<int, string> GetPages(ChapterRecord chapter)
        {
            IDictionary<int, string> pages = new Dictionary<int, string>();

            var document = WebHelper.GetHtmlDocument(chapter.Url);

            var scriptNode = document.SelectSingleNode(@"//div[@id=""image_frame""]/following-sibling::script");
            if (scriptNode == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            var scriptText = scriptNode.InnerText;

            int page = 1;

            foreach (Match match in _pagesRegex.Matches(scriptText))
            {
                pages.Add(page, GetFullUrl(match.Groups[1].Captures[0].Value));

                page++;
            }

            return pages;
        }

        #endregion // Private methods
    }
}

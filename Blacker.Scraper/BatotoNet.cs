using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.Scraper.Models;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;
using System.Text.RegularExpressions;
using log4net;
using Blacker.Scraper.Cache;

namespace Blacker.Scraper
{
    public sealed class BatotoNet : BaseScraper, IScraper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BatotoNet));

        private const string BatotoNetUrl = "http://www.batoto.net";
        private const string SearchUrlFormat = "http://www.batoto.net/search?name={0}&name_cond=c";

        private readonly Cache<string, object> _cache;

        private const string ChapterCacheKey = "Chapters";

        public BatotoNet()
        {
            _cache = new Cache<string, object>();
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("option");
        }

        protected override string BaseUrl
        {
            get { return BatotoNetUrl; }
        }

        #region IScraper implementation

        public string Name { get { return "Batoto.net"; } }

        public Guid ScraperGuid
        {
            get { return Guid.Parse("41675e50-649c-4fbb-b0b9-769ebd8a93b8"); }
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

            var chapters = document.SelectNodes(@"//table[contains(@class, ""chapters_list"")]//tr[contains(@class, ""lang_English"")]/td[1]/a");
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
            if (filter.Length < 3)
                return Enumerable.Empty<MangaRecord>();

            var records = new List<MangaRecord>();
            var document = WebHelper.GetHtmlDocument(String.Format(SearchUrlFormat, Uri.EscapeDataString(filter)));

            var mangas = document.SelectNodes(@"//div[@id=""comic_search_results""]/table//tr/td[1]//a");
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

        public IDownloader GetDownloader()
        {
            return new Downloader(GetPages, @"//img[@id=""comic_page""]");
        }

        #endregion // IScraper implementation

        #region Private methods

        #endregion // Private methods

        private IDictionary<int, string> GetPages(IChapterRecord chapter)
        {
            IDictionary<int, string> pages = new Dictionary<int, string>();

            var document = WebHelper.GetHtmlDocument(chapter.Url);
            var chapterPages = document.SelectNodes(@"(//select[@id=""page_select""])[1]/option");
            if (chapterPages == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var pageLink in chapterPages)
            {
                int pageNumber = 0;

                if(!Int32.TryParse(Regex.Match(pageLink.InnerText, @"\d+").Value, out pageNumber))
                    _log.Error("Unable to parse page number '" + pageLink.InnerText + "'");
    
                if (pages.ContainsKey(pageNumber))  // if page is already in dictionary use random number instead
                    pageNumber = Random;

                pages.Add(pageNumber, GetFullUrl(pageLink.Attributes["value"].Value));
            }

            return pages;
        }
    }
}

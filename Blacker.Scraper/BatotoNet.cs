using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.Scraper.Events;
using System.IO;
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

        private Cache<string, object> _cache;

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

        private Scrapers Scraper
        {
            get { return Scrapers.BatotoNet; }
        }

        #region IScraper implementation

        public string Name { get { return "Batoto.net"; } }

        public IEnumerable<ChapterRecord> GetAvailableChapters(MangaRecord manga)
        {
            if (manga == null)
                throw new ArgumentNullException("manga");
            if (manga.Scraper != Scraper)
                throw new ArgumentException("Manga record is not for " + Scraper.ToString(), "manga");

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
                records.Add(new ChapterRecord(Scrapers.BatotoNet)
                {
                    MangaName = manga.MangaName,
                    ChapterName = System.Net.WebUtility.HtmlDecode(chapter.InnerText.Replace(Environment.NewLine, "").Trim()),
                    Url = GetFullUrl(chapter.Attributes["href"].Value),
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

                records.Add(new MangaRecord(Scrapers.BatotoNet)
                {
                    MangaName = System.Net.WebUtility.HtmlDecode(manga.InnerText.Trim()),
                    Url = GetFullUrl(manga.Attributes["href"].Value)
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

        private IDictionary<int, string> GetPages(ChapterRecord chapter)
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

                try
                {
                    Int32.TryParse(Regex.Match(pageLink.InnerText, @"\d+").Value, out pageNumber);
                }
                catch (Exception ex)
                {
                    _log.Error("Unable to parse page number '" + pageLink.InnerText + "'", ex);
                }

                if (pages.ContainsKey(pageNumber))  // if page is already in dictionary use random number instead
                    pageNumber = Random;

                pages.Add(pageNumber, GetFullUrl(pageLink.Attributes["value"].Value));
            }

            return pages;
        }
    }
}

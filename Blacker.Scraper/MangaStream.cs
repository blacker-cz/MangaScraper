using System;
using System.Collections.Generic;
using System.Linq;
using Blacker.Scraper.Models;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;
using Blacker.Scraper.Cache;
using log4net;

namespace Blacker.Scraper
{
    public sealed class MangaStream : BaseScraper, IScraper, IImmediateSearchProvider
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MangaStream));

        private const string DictionaryUrl = "http://mangastream.com/manga";
        private const string MangaStreamUrl = "http://mangastream.com";

        private Cache<string, object> _cache;

        private const string MangasCacheKey = "Mangas";
        private const string ChapterCacheKey = "Chapters";

        public MangaStream()
        {
            _cache = new Cache<string, object>();
        }

        protected override string BaseUrl
        {
            get { return MangaStreamUrl; }
        }

        private Scrapers Scraper
        {
            get { return Scrapers.MangaStream; }
        }

        #region IScraper implementation

        public string Name { get { return "MangaStream"; } }

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
            var document = WebHelper.GetHtmlDocument(DictionaryUrl);

            var columns = document.SelectNodes(@"//div[@id=""contentwrap-inner""]/table/tr/td");
            if (columns == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var column in columns)
            {
                string mangaName = "";

                if (column.ChildNodes == null) // not sure if they initialize child nodes with empty enumerable when there are no childs, so just to be sure
                    continue;

                foreach (var item in column.ChildNodes)
                {
                    switch (item.Name)
                    {
                        case "strong":
                            mangaName = item.InnerText.Trim();
                            break;
                        case "a":
                            if (mangaName == manga.MangaName)
                            {
                                records.Add(new ChapterRecord(Scrapers.MangaStream)
                                {
                                    MangaName = mangaName,
                                    ChapterName = item.InnerText,
                                    Url = GetFullUrl(item.Attributes.FirstOrDefault(a => a.Name == "href").Value),
                                    MangaRecord = manga
                                });
                            }
                            break;
                        default:
                            break;
                    }
                }
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
            return new Downloader(GetPages, @"//img[@id=""p""]");
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

        #region Private properties

        private IEnumerable<MangaRecord> Mangas
        {
            get
            {
                var mangas = _cache[MangasCacheKey] as IEnumerable<MangaRecord>;
                if (mangas == null)
                    _cache[MangasCacheKey] = LoadAllMangas();

                return _cache[MangasCacheKey] as IEnumerable<MangaRecord>;
            }
        }

        #endregion // Private properties

        #region Private methods

        private IEnumerable<MangaRecord> LoadAllMangas()
        {
            var records = new List<MangaRecord>();
            var document = WebHelper.GetHtmlDocument(DictionaryUrl);

            var columns = document.SelectNodes(@"//div[@id=""contentwrap-inner""]/table/tr/td/strong");
            if (columns == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var column in columns)
            {
                if (string.IsNullOrEmpty(column.InnerText))
                    continue;

                records.Add(new MangaRecord(Scrapers.MangaStream)
                {
                    MangaName = column.InnerText.Trim()
                });
            }

            return records;
        }

        private IDictionary<int, string> GetPages(ChapterRecord chapter)
        {
            IDictionary<int, string> pages = new Dictionary<int, string>();

            var document = WebHelper.GetHtmlDocument(chapter.Url);
            var chapterPages = document.SelectNodes(@"//div[@id=""controls""]/a");
            if (chapterPages == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var pageLink in chapterPages)
            {
                if (pageLink.InnerText.IndexOf("prev", StringComparison.InvariantCultureIgnoreCase) != -1)
                    continue;
                if (pageLink.InnerText.IndexOf("next", StringComparison.InvariantCultureIgnoreCase) != -1)
                    continue;

                int pageNumber = 0;

                Int32.TryParse(pageLink.InnerText, out pageNumber);

                if (pages.ContainsKey(pageNumber))  // if page is already in dictionary use random number instead
                    pageNumber = Random;

                pages.Add(pageNumber, GetFullUrl(pageLink.Attributes.FirstOrDefault(a => a.Name == "href").Value));
            }

            return pages;
        }

        #endregion // Private methods
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.Scraper.Models;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;
using Blacker.Scraper.Cache;
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
                            mangaName = CleanupText(item.InnerText);
                            break;
                        case "a":
                            if (mangaName == manga.MangaName)
                            {
                                var url = GetFullUrl(item.Attributes.First(a => a.Name == "href").Value);

                                records.Add(new ChapterRecord(ScraperGuid, url)
                                {
                                    ChapterName = CleanupText(item.InnerText),
                                    Url = url,
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

        public IEnumerable<IMangaRecord> GetAvailableMangas(string filter)
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

            var columns = document.SelectNodes(@"//div[@id=""contentwrap-inner""]/table/tr/td/strong");
            if (columns == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var column in columns)
            {
                if (string.IsNullOrEmpty(column.InnerText))
                    continue;

                var mangaName = CleanupText(column.InnerText);

                // use manga name as identifier, because we don't have any other unique information that we could use
                records.Add(new MangaRecord(ScraperGuid, mangaName)
                {
                    MangaName = mangaName
                });
            }

            return records;
        }

        private IDictionary<int, string> GetPages(IChapterRecord chapter)
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

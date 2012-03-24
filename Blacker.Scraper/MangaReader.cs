using System;
using System.Collections.Generic;
using System.Linq;
using Blacker.Scraper.Models;
using System.IO;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;
using Blacker.Scraper.Events;
using Ionic.Zip;
using Blacker.Scraper.Cache;

namespace Blacker.Scraper
{
    public class MangaReader : IScraper, IImmediateSearchProvider
    {
        private const string DictionaryUrl = "http://mangareader.net/alphabetical";
        private const string MangaReaderUrl = "http://mangareader.net";

        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        private int _tasksCount = 0;
        private int _tasksDone = 0;

        private readonly Random _randomGenerator = new Random(Environment.TickCount);

        private Cache<string, object> _cache;

        private const string MangasCacheKey = "Mangas";
        private const string ChapterCacheKey = "Chapters";

        public MangaReader()
        {
            _cache = new Cache<string, object>();
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("option");
        }

        #region IScraper implementation

        public string Name { get { return "MangaReader.net"; } }

        public IEnumerable<ChapterRecord> GetAvailableChapters(MangaRecord manga)
        {
            var cacheKey = ChapterCacheKey + manga.MangaName + manga.Url;

            var cached = _cache[cacheKey] as IEnumerable<ChapterRecord>;
            if (cached != null) // if chapters are already in cache return them
                return cached;

            var records = new List<ChapterRecord>();
            var document = WebHelper.GetHtmlDocument(manga.Url);

            var chapters = document.SelectNodes(@"//table[@id=""listing""]/tr/td[1]");
            if (chapters == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var chapter in chapters)
            {
                records.Add(new ChapterRecord(Scrapers.MangaReader)
                {
                    MangaName = manga.MangaName,
                    ChapterName = chapter.InnerText.Replace(Environment.NewLine, "").Trim(),
                    Url = GetFullUrl(chapter.ChildNodes.FirstOrDefault(n => n.Name == "a").Attributes["href"].Value),
                    MangaRecord = manga
                });
            }

            // save to cache
            _cache[cacheKey] = records;

            return records;
        }

        public IEnumerable<MangaRecord> GetAvailableMangas()
        {
            return GetAvailableMangas("");
        }

        public IEnumerable<MangaRecord> GetAvailableMangas(string filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            return Mangas.Where(mr => mr.MangaName.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        public void DownloadChapter(ChapterRecord chapter, FileInfo file)
        {
            if (chapter == null)
                throw new ArgumentNullException("chapter");
            if (file == null)
                throw new ArgumentNullException("file");

            // add task -> zip file
            _tasksCount++;

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            DownloadChapter(chapter, directory);

            OnDownloadProgressChanged(new DownloadProgressEventArgs()
            {
                Done = _tasksDone,
                From = _tasksCount,
                Action = String.Format("Compressing chapter to output file")
            });

            try
            {
                using (var zip = new ZipFile())
                {
                    zip.AddDirectory(directory.FullName, null);
                    zip.Save(file.FullName);
                }

                _tasksDone++;
                OnDownloadProgressChanged(new DownloadProgressEventArgs()
                {
                    Done = _tasksDone,
                    From = _tasksCount,
                    Action = String.Format("Download completed")
                });

            }
            finally
            {
                // remove temp dir
                directory.Delete(true);
            }
        }

        public void DownloadChapter(ChapterRecord chapter, DirectoryInfo directory, bool createDir = true)
        {
            if (chapter == null)
                throw new ArgumentNullException("chapter");
            if (directory == null)
                throw new ArgumentNullException("directory");
            if (chapter.Scraper != Scrapers.MangaReader)
                throw new ArgumentException("Chapter record is not for MangaReader.", "chapter");
            if (!createDir && !directory.Exists)
                throw new ArgumentException("Specified directory does not exists.", "directory");

            if (createDir && !directory.Exists)
            {
                directory.Create();
            }

            _tasksCount++;
            OnDownloadProgressChanged(new DownloadProgressEventArgs()
            {
                Done = _tasksDone,
                From = _tasksCount,
                Action = String.Format("Resolving list of pages.")
            });

            var pages = GetPages(chapter);

            _tasksCount += pages.Count;

            _tasksDone++;
            OnDownloadProgressChanged(new DownloadProgressEventArgs()
            {
                Done = _tasksDone,
                From = _tasksCount,
                Action = String.Format("List of pages resolved, chapter has {0} pages.", pages.Count)
            });

            int done = 0;

            foreach (var page in pages)
            {

                OnDownloadProgressChanged(new DownloadProgressEventArgs()
                {
                    Done = _tasksDone,
                    From = _tasksCount,
                    Action = String.Format("Downloading page {0} from {1}", done, pages.Count)
                });

                string imgUrl = GetPageImageUrl(page.Value);
                string filePath = GetUniqueFileName(directory.FullName, page.Key, Path.GetExtension(imgUrl));

                try
                {
                    WebHelper.DownloadImage(imgUrl, filePath);
                }
                catch (Exception)
                {
                    // todo: this should be logged
                }

                done++;
                _tasksDone++;
            }

            OnDownloadProgressChanged(new DownloadProgressEventArgs()
            {
                Done = _tasksDone,
                From = _tasksCount,
                Action = String.Format("All pages downloaded.")
            });

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

            var mangas = document.SelectNodes(@"//ul[@class=""series_alpha""]/li/a");
            if (mangas == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var manga in mangas)
            {
                if (string.IsNullOrEmpty(manga.InnerText))
                    continue;

                records.Add(new MangaRecord(Scrapers.MangaReader)
                {
                    MangaName = manga.InnerText.Trim(),
                    Url = GetFullUrl(manga.Attributes["href"].Value)
                });
            }

            return records;
        }

        private IDictionary<int, string> GetPages(ChapterRecord chapter)
        {
            IDictionary<int, string> pages = new Dictionary<int, string>();

            var document = WebHelper.GetHtmlDocument(chapter.Url);
            var chapterPages = document.SelectNodes(@"//select[@id=""pageMenu""]/option");
            if (chapterPages == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var pageLink in chapterPages)
            {
                int pageNumber = 0;

                Int32.TryParse(pageLink.InnerText, out pageNumber);

                if (pages.ContainsKey(pageNumber))  // if page is already in dictionary use random number instead
                    pageNumber = _randomGenerator.Next(0, Int32.MaxValue);

                pages.Add(pageNumber, GetFullUrl(pageLink.Attributes["value"].Value));
            }

            return pages;
        }

        private string GetPageImageUrl(string pageUrl)
        {
            var document = WebHelper.GetHtmlDocument(pageUrl);
            var img = document.SelectSingleNode(@"//img[@id=""img""]");
            if (img == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            return img.Attributes.FirstOrDefault(a => a.Name == "src").Value;
        }

        private string GetFullUrl(string url, string urlBase = MangaReaderUrl)
        {
            var baseUri = new Uri(urlBase);
            Uri uri;

            if (Uri.TryCreate(baseUri, url, out uri))
            {
                return uri.AbsoluteUri;
            }
            return url;
        }

        private string GetUniqueFileName(string directory, int page, string extension)
        {
            string filePath = Path.Combine(directory, page + extension);
            int counter = 0;

            while (File.Exists(filePath))
            {
                ++counter;
                filePath = Path.Combine(directory, page + "[" + counter + "]" + extension);
            }

            return filePath;
        }

        private void OnDownloadProgressChanged(DownloadProgressEventArgs e)
        {
            if (DownloadProgress != null)
            {
                DownloadProgress(this, e);
            }
        }

        #endregion // Private methods
    }
}

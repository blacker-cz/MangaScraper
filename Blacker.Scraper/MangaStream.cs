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
    public class MangaStream : IScraper, IImmediateSearchProvider
    {
        private const string DictionaryUrl = "http://mangastream.com/manga";
        private const string MangaStreamUrl = "http://mangastream.com";

        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        private int _tasksCount = 0;
        private int _tasksDone = 0;

        private readonly Random _randomGenerator = new Random(Environment.TickCount);

        private Cache<string, object> _cache;

        private const string MangasCacheKey = "Mangas";
        private const string ChapterCacheKey = "Chapters";

        public MangaStream()
        {
            _cache = new Cache<string, object>();
        }

        #region IScraper implementation

        public string Name { get { return "MangaStream"; } }

        public IEnumerable<ChapterRecord> GetAvailableChapters(MangaRecord manga)
        {
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
            if (chapter.Scraper != Scrapers.MangaStream)
                throw new ArgumentException("Chapter record is not for MangaStream.", "chapter");
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
                    pageNumber = _randomGenerator.Next(0, Int32.MaxValue);

                pages.Add(pageNumber, GetFullUrl(pageLink.Attributes.FirstOrDefault(a => a.Name == "href").Value));
            }

            return pages;
        }

        private string GetPageImageUrl(string pageUrl)
        {
            var document = WebHelper.GetHtmlDocument(pageUrl);
            var img = document.SelectSingleNode(@"//img[@id=""p""]");
            if (img == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            return img.Attributes.FirstOrDefault(a => a.Name == "src").Value;
        }

        private string GetFullUrl(string url, string urlBase = MangaStreamUrl)
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

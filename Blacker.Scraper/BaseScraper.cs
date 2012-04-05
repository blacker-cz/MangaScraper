using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.Scraper.Events;
using System.IO;
using Blacker.Scraper.Cache;
using Blacker.Scraper.Models;
using Ionic.Zip;
using Blacker.Scraper.Helpers;
using log4net;

namespace Blacker.Scraper
{
    public abstract class BaseScraper : IDownloadProvider, IDownloadProgressReporter
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BaseScraper));

        #region IDownloadProgressReporter implementation

        public event EventHandler<Events.DownloadProgressEventArgs> DownloadProgress;

        #endregion // IDownloadProgressReporter implementation

        #region Download progress reporting stuff

        private int _tasksCount = 0;
        private int _tasksDone = 0;

        protected void AddTask(int num = 1)
        {
            _tasksCount += num;
        }

        protected void TaskDone(int num = 1)
        {
            _tasksDone += num;
        }

        protected virtual void ReportProgress(string message, params object[] args)
        {
            OnDownloadProgressChanged(new DownloadProgressEventArgs()
                            {
                                From = _tasksCount,
                                Done = _tasksDone,
                                Action = String.Format(message, args)
                            });

            // if there are no other tasks to do we can reset counters
            if (_tasksCount == _tasksDone)
                ResetTasks();
        }

        protected void OnDownloadProgressChanged(DownloadProgressEventArgs e)
        {
            if (DownloadProgress != null)
            {
                DownloadProgress(this, e);
            }
        }

        protected void ResetTasks()
        {
            _tasksCount = 0;
            _tasksDone = 0;
        }

        #endregion // Download progress reporting stuff 

        #region Random numbers generator

        private readonly Random _randomGenerator = new Random(Environment.TickCount);

        protected int Random { get { return _randomGenerator.Next(0, Int32.MaxValue); } }

        #endregion // Random numbers generator

        #region IDownloadProvider implementation

        public virtual void DownloadChapter(ChapterRecord chapter, FileInfo file)
        {
            if (chapter == null)
                throw new ArgumentNullException("chapter");
            if (file == null)
                throw new ArgumentNullException("file");

            // add task -> zip file
            AddTask();

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            DownloadChapter(chapter, directory);

            ReportProgress("Compressing chapter to output file");

            try
            {
                using (var zip = new ZipFile())
                {
                    zip.AddDirectory(directory.FullName, null);
                    zip.Save(file.FullName);
                }

                TaskDone();
                ReportProgress("Download completed");
            }
            finally
            {
                // remove temp dir
                directory.Delete(true);
            }
        }

        public virtual void DownloadChapter(ChapterRecord chapter, DirectoryInfo directory, bool createDir = true)
        {
            if (chapter == null)
                throw new ArgumentNullException("chapter");
            if (directory == null)
                throw new ArgumentNullException("directory");
            if (chapter.Scraper != Scraper)
                throw new ArgumentException("Chapter record is not for " + Scraper.ToString(), "chapter");
            if (!createDir && !directory.Exists)
                throw new ArgumentException("Specified directory does not exists.", "directory");

            if (createDir && !directory.Exists)
            {
                directory.Create();
            }

            AddTask();
            ReportProgress("Resolving list of pages.");

            var pages = GetPages(chapter);

            AddTask(pages.Count);

            TaskDone();
            ReportProgress("List of pages resolved, chapter has {0} pages.", pages.Count);

            int done = 0;

            foreach (var page in pages)
            {

                ReportProgress("Downloading page {0} from {1}", done, pages.Count);

                string imgUrl = GetPageImageUrl(page.Value);
                string filePath = GetUniqueFileName(directory.FullName, page.Key, Path.GetExtension(imgUrl));

                try
                {
                    WebHelper.DownloadImage(imgUrl, filePath);
                }
                catch (Exception ex)
                {
                    _log.Error("Unable to download image from url: '" + imgUrl + "' to '" + filePath + "'", ex);
                }

                done++;
                TaskDone();
            }

            ReportProgress("All pages downloaded.");
        }

        #endregion  // IDownloadProvider implementation

        protected abstract string BaseUrl { get; }

        protected abstract Scrapers Scraper { get; }

        /// <summary>
        /// Get pages for the chapter record.
        /// </summary>
        /// <param name="chapter">Chapter for which to get pages</param>
        /// <returns>Dictionary where key is page number and value is page url</returns>
        protected abstract IDictionary<int, string> GetPages(ChapterRecord chapter);

        /// <summary>
        /// Get image url from page url
        /// </summary>
        /// <param name="pageUrl">Page url</param>
        /// <returns>Image url</returns>
        protected abstract string GetPageImageUrl(string pageUrl);

        protected string GetFullUrl(string url)
        {
            return GetFullUrl(url, BaseUrl);
        }

        protected string GetFullUrl(string url, string urlBase)
        {
            var baseUri = new Uri(urlBase);
            Uri uri;

            if (Uri.TryCreate(baseUri, url, out uri))
            {
                return uri.AbsoluteUri;
            }
            return url;
        }

        protected string GetUniqueFileName(string directory, int page, string extension)
        {
            return GetUniqueFileName(directory, page.ToString("D3"), extension);
        }

        protected string GetUniqueFileName(string directory, string page, string extension)
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
    }
}

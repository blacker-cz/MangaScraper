using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Events;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Common.Utils;
using Blacker.Scraper.Models;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;
using System.IO;
using Ionic.Zip;
using log4net;
using System.ComponentModel;

namespace Blacker.Scraper
{
    public class Downloader : IDownloader
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Downloader));

        private readonly object _syncRoot = new object();

        private readonly Func<string, string> _imageFinder;
        private readonly Func<IChapterRecord, IDictionary<int, string>> _pageResolver;

        private readonly BackgroundWorker _backgroundWorker;

        #region Constructors

        public Downloader(IDictionary<int, string> pages, string imageXPath)
            : this(pages, (x) => { return GetPageImageUrl(imageXPath, x); })
        {
            if (string.IsNullOrEmpty(imageXPath))
                throw new ArgumentException("Invalid XPath", "imageXPath");
        }

        public Downloader(IDictionary<int, string> pages, Func<string, string> imageFinder)
            : this((x) => { return pages; }, imageFinder)
        {
            if (pages == null)
                throw new ArgumentNullException("pages");
        }

        public Downloader(Func<IChapterRecord, IDictionary<int, string>> pagesResolver, string imageXPath)
            : this(pagesResolver, (x) => { return GetPageImageUrl(imageXPath, x); })
        {
            if (string.IsNullOrEmpty(imageXPath))
                throw new ArgumentException("Invalid XPath", "imageXPath");
        }

        public Downloader(Func<IChapterRecord, IDictionary<int, string>> pagesResolver, Func<string, string> imageFinder)
        {
            if (pagesResolver == null)
                throw new ArgumentNullException("pagesResolver");
            if (imageFinder == null)
                throw new ArgumentNullException("imageFinder");

            _pageResolver = pagesResolver;
            _imageFinder = imageFinder;

            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += _backgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += _backgroundWorker_RunWorkerCompleted;
            _backgroundWorker.ProgressChanged += _backgroundWorker_ProgressChanged;
        }

        #endregion // Constructors

        #region IDownloadProgressReporter implementation

        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        #endregion // IDownloadProgressReporter implementation

        #region Download progress reporting stuff

        private int _tasksCount = 0;
        private int _tasksDone = 0;

        protected void AddTask(int num = 1)
        {
            lock (_syncRoot)
            {
                _tasksCount += num;
            }
        }

        protected void TaskDone(int num = 1)
        {
            lock (_syncRoot)
            {
                _tasksDone += num;
            }
        }

        protected int GetPercentComplete()
        {
            lock (_syncRoot)
            {
                return (int)(((float)_tasksDone / _tasksCount) * 100);
            }
        }

        protected void ResetTasks()
        {
            lock (_syncRoot)
            {
                _tasksCount = 0;
                _tasksDone = 0;
            }
        }

        protected void ReportProgress(string message, params object[] args)
        {
            ReportProgress(GetPercentComplete(), message, args);
        }

        protected void ReportProgress(int percentComplete, string message, params object[] args)
        {
            OnDownloadProgressChanged(new DownloadProgressEventArgs()
            {
                PercentComplete = percentComplete,
                Message = String.Format(message, args)
            });

            // if there are no other tasks to do we can reset counters
            lock (_syncRoot)
            {
                if (_tasksCount == _tasksDone)
                    ResetTasks();
            }
        }

        protected void OnDownloadProgressChanged(DownloadProgressEventArgs e)
        {
            if (DownloadProgress != null)
            {
                DownloadProgress(this, e);
            }
        }

        #endregion // Download progress reporting stuff 

        #region IDownloadProvider implementation

        public virtual void DownloadChapterAsync(ISemaphore semaphore, IChapterRecord chapter, string outputFolder, IDownloadFormatProvider formatProvider)
        {
            if (_backgroundWorker.IsBusy)
                throw new InvalidOperationException("Download is currently in progress.");

            if (semaphore == null)
                throw new ArgumentNullException("semaphore");
            if (chapter == null)
                throw new ArgumentNullException("chapter");
            if (String.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("Output folder must not be null or empty.", "outputFolder");
            if (formatProvider == null) 
                throw new ArgumentNullException("formatProvider");

            var workerParams = new WorkerParams()
            {
                Chapter = chapter,
                Semaphore = semaphore,
                OutputFolder = outputFolder,
                FormatProvider = formatProvider
            };

            _backgroundWorker.RunWorkerAsync(workerParams);
        }

        public void Cancel()
        {
            _backgroundWorker.CancelAsync();
        }

        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;

        #endregion  // IDownloadProvider implementation

        #region Background worker

        void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var workerParams = e.Argument as WorkerParams;
            var backgroundWorker = sender as BackgroundWorker;

            backgroundWorker.ReportProgress(0, "Waiting");

            bool obtained = workerParams.Semaphore.Wait();  // we don't really care if we obtained entry pass or not

            try
            {
                DownloadChapter(backgroundWorker, e, workerParams.Chapter, workerParams.OutputFolder, workerParams.FormatProvider);
            }
            finally
            {
                if(obtained)
                    workerParams.Semaphore.Release();
            }
        }

        void _backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnDownloadCompleted(new DownloadCompletedEventArgs()
                    {
                        Cancelled = e.Cancelled,
                        Error = e.Error,
                        DownloadedPath = e.Result as string
                    });
        }

        void _backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ReportProgress(e.ProgressPercentage, e.UserState as string);
        }

        private void DownloadChapter(BackgroundWorker backgroundWorker, DoWorkEventArgs e, IChapterRecord chapter, string outputFolder, IDownloadFormatProvider formatProvider)
        {
            // add task -> export result
            AddTask();

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "MangaScraper", Guid.NewGuid().ToString()));

            try
            {
                AddTask();
                backgroundWorker.ReportProgress(GetPercentComplete(), "Resolving list of pages.");

                var pages = _pageResolver(chapter);

                AddTask(pages.Count);

                TaskDone();
                backgroundWorker.ReportProgress(GetPercentComplete(), String.Format("List of pages resolved, chapter has {0} pages.", pages.Count));

                int current = 1;

                foreach (var page in pages)
                {

                    if (backgroundWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    backgroundWorker.ReportProgress(GetPercentComplete(), String.Format("Downloading page {0} from {1}", current, pages.Count));

                    string imgUrl = _imageFinder(page.Value);
                    string filePath = GetUniqueFileName(directory.FullName, page.Key, Path.GetExtension(imgUrl));

                    try
                    {
                        RetryHelper.Retry(() => WebHelper.DownloadImage(imgUrl, filePath));
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Unable to download image from url: '" + imgUrl + "' to '" + filePath + "'", ex);
                    }

                    current++;
                    TaskDone();
                }

                backgroundWorker.ReportProgress(GetPercentComplete(), "All pages downloaded.");
                backgroundWorker.ReportProgress(GetPercentComplete(), "Exporting chapter");

                if (backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                string path;
                formatProvider.SaveDownloadedChapter(chapter, directory, outputFolder, out path);

                // save result path of the downloaded file
                e.Result = path;

                TaskDone();
                backgroundWorker.ReportProgress(GetPercentComplete(), "Download completed");
            }
            finally
            {
                // remove temp dir
                directory.Delete(true);
            }
        }

        #endregion // Background worker

        protected void OnDownloadCompleted(DownloadCompletedEventArgs eventArgs)
        {
            var dcEvent = DownloadCompleted;
            if (dcEvent != null)
            {
                dcEvent(this, eventArgs);
            }
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

        private static string GetPageImageUrl(string imageXPath, string pageUrl)
        {
            var document = WebHelper.GetHtmlDocument(pageUrl);
            var img = document.SelectSingleNode(imageXPath);
            if (img == null || !img.Attributes.Contains("src"))
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            return img.Attributes.First(a => a.Name == "src").Value;
        }

        private class WorkerParams
        {
            public IChapterRecord Chapter { get; set; }
            public ISemaphore Semaphore { get; set; }
            public string OutputFolder { get; set; }
            public IDownloadFormatProvider FormatProvider { get; set; }
        }
    }
}

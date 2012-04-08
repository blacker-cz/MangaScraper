using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.Scraper.Models;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;
using System.IO;
using Ionic.Zip;
using Blacker.Scraper.Events;
using log4net;
using System.ComponentModel;

namespace Blacker.Scraper
{
    public interface IDownloader : IDownloadProvider, IDownloadProgressReporter { }

    public class Downloader : IDownloader
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Downloader));

        private readonly object _syncRoot = new object();

        private readonly Func<string, string> _imageFinder;
        private readonly Func<ChapterRecord, IDictionary<int, string>> _pageResolver;

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

        public Downloader(Func<ChapterRecord, IDictionary<int, string>> pagesResolver, string imageXPath)
            : this(pagesResolver, (x) => { return GetPageImageUrl(imageXPath, x); })
        {
            if (string.IsNullOrEmpty(imageXPath))
                throw new ArgumentException("Invalid XPath", "imageXPath");
        }

        public Downloader(Func<ChapterRecord, IDictionary<int, string>> pagesResolver, Func<string, string> imageFinder)
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

        public event EventHandler<Events.DownloadProgressEventArgs> DownloadProgress;

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

        public virtual void DownloadChapterAsync(ChapterRecord chapter, FileInfo file)
        {
            if (_backgroundWorker.IsBusy)
                throw new InvalidOperationException("Download is currently in progress.");

            if (chapter == null)
                throw new ArgumentNullException("chapter");
            if (file == null)
                throw new ArgumentNullException("file");

            var workerParams = new WorkerParams()
                    {
                        Chapter = chapter,
                        IsFile = true,
                        File = file
                    };

            _backgroundWorker.RunWorkerAsync(workerParams);
        }

        public virtual void DownloadChapterAsync(ChapterRecord chapter, DirectoryInfo directory, bool createDir = true)
        {
            if (_backgroundWorker.IsBusy)
                throw new InvalidOperationException("Download is currently in progress.");

            if (chapter == null)
                throw new ArgumentNullException("chapter");
            if (directory == null)
                throw new ArgumentNullException("directory");
            if (!createDir && !directory.Exists)
                throw new ArgumentException("Specified directory does not exists.", "directory");

            var workerParams = new WorkerParams()
                    {
                        Chapter = chapter,
                        IsFile = false,
                        Directory = directory,
                        CreateDirectory = createDir
                    };

            _backgroundWorker.RunWorkerAsync(workerParams);
        }

        public void Cancel()
        {
            _backgroundWorker.CancelAsync();
        }

        public event EventHandler<Events.DownloadCompletedEventArgs> DownloadCompleted;

        #endregion  // IDownloadProvider implementation

        #region Background worker

        void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var workerParams = e.Argument as WorkerParams;
            var backgroundWorker = sender as BackgroundWorker;

            if (workerParams.IsFile)
                DownloadChapter(backgroundWorker, e, workerParams.Chapter, workerParams.File);
            else
                DownloadChapter(backgroundWorker, e, workerParams.Chapter, workerParams.Directory, workerParams.CreateDirectory);
        }

        void _backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnDownloadCompleted(new DownloadCompletedEventArgs()
                    {
                        Cancelled = e.Cancelled,
                        Error = e.Error
                    });
        }

        void _backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ReportProgress(e.ProgressPercentage, e.UserState as string);
        }

        private void DownloadChapter(BackgroundWorker backgroundWorker, DoWorkEventArgs e, ChapterRecord chapter, FileInfo file)
        {
            // add task -> zip file
            AddTask();

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            DownloadChapter(backgroundWorker, e, chapter, directory);

            backgroundWorker.ReportProgress(GetPercentComplete(), "Compressing chapter to output file");

            try
            {
                if (backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                using (var zip = new ZipFile())
                {
                    zip.AddDirectory(directory.FullName, null);
                    zip.Save(file.FullName);
                }

                TaskDone();
                backgroundWorker.ReportProgress(GetPercentComplete(), "Download completed");
            }
            finally
            {
                // remove temp dir
                directory.Delete(true);
            }
        }

        private void DownloadChapter(BackgroundWorker backgroundWorker, DoWorkEventArgs e, ChapterRecord chapter, DirectoryInfo directory, bool createDir = true)
        {
            if (createDir && !directory.Exists)
            {
                directory.Create();
            }

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
                    WebHelper.DownloadImage(imgUrl, filePath);
                }
                catch (Exception ex)
                {
                    _log.Error("Unable to download image from url: '" + imgUrl + "' to '" + filePath + "'", ex);
                }

                current++;
                TaskDone();
            }

            backgroundWorker.ReportProgress(GetPercentComplete(), "All pages downloaded.");
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
            if (img == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            return img.Attributes.FirstOrDefault(a => a.Name == "src").Value;
        }

        private class WorkerParams
        {
            public ChapterRecord Chapter { get; set; }
            public bool IsFile { get; set; }
            public FileInfo File { get; set; }
            public DirectoryInfo Directory { get; set; }
            public bool CreateDirectory { get; set; }
        }
    }
}

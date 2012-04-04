using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.Scraper.Events;
using System.IO;
using Blacker.Scraper.Cache;

namespace Blacker.Scraper
{
    public abstract class BaseScraper : IDownloadProgressReporter
    {
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

        protected abstract string BaseUrl { get; }

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

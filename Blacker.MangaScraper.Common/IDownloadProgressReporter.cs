using System;
using Blacker.MangaScraper.Common.Events;

namespace Blacker.MangaScraper.Common
{
    public interface IDownloadProgressReporter
    {
        /// <summary>
        /// Event signalling download progress
        /// </summary>
        event EventHandler<DownloadProgressEventArgs> DownloadProgress;
    }
}

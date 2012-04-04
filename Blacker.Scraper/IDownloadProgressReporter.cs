using System;
namespace Blacker.Scraper
{
    public interface IDownloadProgressReporter
    {
        /// <summary>
        /// Event signalling download progress
        /// </summary>
        event EventHandler<Blacker.Scraper.Events.DownloadProgressEventArgs> DownloadProgress;
    }
}

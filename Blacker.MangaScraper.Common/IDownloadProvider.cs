using System;
using System.IO;
using Blacker.MangaScraper.Common.Events;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Common.Utils;

namespace Blacker.MangaScraper.Common
{
    public interface IDownloadProvider
    {
        /// <summary>
        /// Download chapter to file compressed using ZIP algorithm
        /// </summary>
        /// <param name="semaphore">Semaphore used to limit maximal number of simultaneous downloads</param>
        /// <param name="chapter">Chapter info</param>
        /// <param name="outputFolder">Output folder</param>
        /// <param name="formatProvider">Output format provider</param>
        void DownloadChapterAsync(ISemaphore semaphore, IChapterRecord chapter, string outputFolder, IDownloadFormatProvider formatProvider);

        /// <summary>
        /// Cancel the chapter download
        /// </summary>
        void Cancel();

        /// <summary>
        /// Event signalling download progress
        /// </summary>
        event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;
    }
}

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
        /// <param name="file">Output file info</param>
        void DownloadChapterAsync(ISemaphore semaphore, IChapterRecord chapter, FileInfo file);

        /// <summary>
        /// Download chapter to directory
        /// </summary>
        /// <param name="semaphore">Semaphore used to limit maximal number of simultaneous downloads</param>
        /// <param name="chapter">Chapter info</param>
        /// <param name="directory">Output directory info</param>
        /// <param name="createDir">Flag if directory should be created if it does not exist (optional, defaul true)</param>
        void DownloadChapterAsync(ISemaphore semaphore, IChapterRecord chapter, DirectoryInfo directory, bool createDir = true);

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

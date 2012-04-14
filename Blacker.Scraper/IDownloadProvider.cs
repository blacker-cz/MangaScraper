﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.Scraper.Models;
using System.IO;
using System.Threading;
using Blacker.Scraper.Utils;

namespace Blacker.Scraper
{
    public interface IDownloadProvider
    {
        /// <summary>
        /// Download chapter to file compressed using ZIP algorithm
        /// </summary>
        /// <param name="semaphore">Semaphore used to limit maximal number of simultaneous downloads</param>
        /// <param name="chapter">Chapter info</param>
        /// <param name="file">Output file info</param>
        void DownloadChapterAsync(ISemaphore semaphore, ChapterRecord chapter, FileInfo file);

        /// <summary>
        /// Download chapter to directory
        /// </summary>
        /// <param name="semaphore">Semaphore used to limit maximal number of simultaneous downloads</param>
        /// <param name="chapter">Chapter info</param>
        /// <param name="directory">Output directory info</param>
        /// <param name="createDir">Flag if directory should be created if it does not exist (optional, defaul true)</param>
        void DownloadChapterAsync(ISemaphore semaphore, ChapterRecord chapter, DirectoryInfo directory, bool createDir = true);

        /// <summary>
        /// Cancel the chapter download
        /// </summary>
        void Cancel();

        /// <summary>
        /// Event signalling download progress
        /// </summary>
        event EventHandler<Events.DownloadCompletedEventArgs> DownloadCompleted;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.Scraper.Models;
using System.IO;

namespace Blacker.Scraper
{
    public interface IDownloadProvider
    {
        /// <summary>
        /// Download chapter to file compressed using ZIP algorithm
        /// </summary>
        /// <param name="chapter">Chapter info</param>
        /// <param name="file">Output file info</param>
        void DownloadChapter(ChapterRecord chapter, FileInfo file);

        /// <summary>
        /// Download chapter to directory
        /// </summary>
        /// <param name="chapter">Chapter info</param>
        /// <param name="directory">Output directory info</param>
        /// <param name="createDir">Flag if directory should be created if it does not exist (optional, defaul true)</param>
        void DownloadChapter(ChapterRecord chapter, DirectoryInfo directory, bool createDir = true);
    }
}

using System;
using System.IO;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.MangaScraper.Common
{
    public interface IDownloadFormatProvider
    {
        /// <summary>
        /// Priority/order in which the providers should be loaded
        /// </summary>
        ushort Priority { get; }

        /// <summary>
        /// Unique identifier for format provider implementation
        /// </summary>
        Guid FormatGuid { get; }

        /// <summary>
        /// Name of the format (e.g. Folder, ZIP, CBZ, ...)
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Save the downloaded chapter
        /// </summary>
        /// <param name="chapter">Chapter record</param>
        /// <param name="downloadedFiles">Directory containing all downloaded files</param>
        /// <param name="outputFolder">Output folder</param>
        /// <param name="path">Path to the saved output (file/folder/etc.)</param>
        void SaveDownloadedChapter(IChapterRecord chapter, DirectoryInfo downloadedFiles, string outputFolder, out string path);
    }
}

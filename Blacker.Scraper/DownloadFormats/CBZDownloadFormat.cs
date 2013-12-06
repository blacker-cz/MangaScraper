using System;
using System.IO;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Ionic.Zip;

namespace Blacker.Scraper.DownloadFormats
{
    public class CBZDownloadFormat : BaseDownloadFormat, IDownloadFormatProvider
    {
        public ushort Priority 
        {
            get { return 10; }
        }

        public Guid FormatGuid
        {
            get { return Guid.Parse("F152F015-177A-405D-A9A5-3D97DE13D9A4"); }
        }

        public string FormatName
        {
            get { return "Comic Book (CBZ)"; }
        }

        public void SaveDownloadedChapter(IChapterRecord chapter, DirectoryInfo downloadedFiles, string outputFolder, out string path)
        {
            if (chapter == null)
                throw new ArgumentNullException("chapter");
            if (downloadedFiles == null)
                throw new ArgumentNullException("downloadedFiles");
            if (String.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("Output path must not be null or empty.", "outputFolder");

            path = Path.Combine(outputFolder, GetNameForSave(chapter) + ".cbz");

            var fileInfo = new FileInfo(path);

            using (var zip = new ZipFile())
            {
                zip.AddDirectory(downloadedFiles.FullName, null);
                zip.Save(fileInfo.FullName);
            }
        }
    }
}

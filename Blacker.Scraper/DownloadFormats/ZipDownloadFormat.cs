using System;
using System.IO;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Ionic.Zip;

namespace Blacker.Scraper.DownloadFormats
{
    public class ZipDownloadFormat : BaseDownloadFormat, IDownloadFormatProvider
    {
        public ushort Priority
        {
            get { return 1; }
        }

        public Guid FormatGuid
        {
            get { return Guid.Parse("45445BD5-EBED-4F85-BFE1-0CAC718C2642"); }
        }

        public string FormatName
        {
            get { return "Compressed File (ZIP)"; }
        }

        public void SaveDownloadedChapter(IChapterRecord chapter, DirectoryInfo downloadedFiles, string outputFolder, out string path)
        {
            if (chapter == null)
                throw new ArgumentNullException("chapter");
            if (downloadedFiles == null)
                throw new ArgumentNullException("downloadedFiles");
            if (String.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("Output path must not be null or empty.", "outputFolder");

            path = Path.Combine(outputFolder, GetNameForSave(chapter) + ".zip");

            var fileInfo = new FileInfo(path);

            using (var zip = new ZipFile())
            {
                zip.AddDirectory(downloadedFiles.FullName, null);
                zip.Save(fileInfo.FullName);
            }
        }
    }
}

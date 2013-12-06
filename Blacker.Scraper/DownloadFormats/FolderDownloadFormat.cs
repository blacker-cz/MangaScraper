using System;
using System.IO;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.Scraper.DownloadFormats
{
    public class FolderDownloadFormat : BaseDownloadFormat, IDownloadFormatProvider
    {
        public ushort Priority
        {
            get { return 20; }
        }

        public Guid FormatGuid
        {
            get { return Guid.Parse("30E047F6-8568-4A0A-95B1-D1796C7C3F30"); }
        }

        public string FormatName
        {
            get { return "Folder"; }
        }

        public void SaveDownloadedChapter(IChapterRecord chapter, DirectoryInfo downloadedFiles, string outputFolder, out string path)
        {
            if (chapter == null) 
                throw new ArgumentNullException("chapter");
            if (downloadedFiles == null) 
                throw new ArgumentNullException("downloadedFiles");
            if (String.IsNullOrEmpty(outputFolder)) 
                throw new ArgumentException("Output path must not be null or empty.", "outputFolder");

            var outputDir = new DirectoryInfo(Path.Combine(outputFolder, GetNameForSave(chapter)));

            path = outputDir.FullName;

            DirectoryCopy(downloadedFiles.FullName, path, true);
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            var dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            foreach (FileInfo file in dir.GetFiles())
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}

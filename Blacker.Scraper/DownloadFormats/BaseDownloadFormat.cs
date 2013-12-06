using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.Scraper.DownloadFormats
{
    public abstract class BaseDownloadFormat
    {
        private static readonly Regex InvalidPathCharsRegex =
            new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))),
                      RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        protected string GetNameForSave(IChapterRecord chapter)
        {
            string fileName = String.Format("{0} - {1}", chapter.MangaName, chapter.ChapterName).Replace(" ", "_");

            return InvalidPathCharsRegex.Replace(fileName, "");
        }
    }
}

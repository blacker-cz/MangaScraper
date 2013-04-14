using System;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.MangaScraper.Library.Models
{
    /// <summary>
    /// Class wrapping downloaded chapter record and keeping additional information about download.
    /// </summary>
    public class DownloadedChapterInfo
    {
        public DownloadedChapterInfo(IChapterRecord chapterRecord)
        {
            if (chapterRecord == null) 
                throw new ArgumentNullException("chapterRecord");

            ChapterRecord = chapterRecord;
            Path = String.Empty;
            Downloaded = DateTime.MinValue;
        }

        /// <summary>
        /// Chapter record
        /// </summary>
        public IChapterRecord ChapterRecord { get; private set; }

        /// <summary>
        /// Path where the downloaded chapter can be found.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Time when the chapter was downloaded
        /// </summary>
        public DateTime Downloaded { get; set; }

        /// <summary>
        /// Flag if the downloaded file is zipped
        /// </summary>
        public bool IsZip { get; set; }
    }
}

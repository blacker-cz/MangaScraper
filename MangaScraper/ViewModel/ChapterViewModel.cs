using System;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Library;
using Blacker.MangaScraper.Library.Models;

namespace Blacker.MangaScraper.ViewModel
{
    internal class ChapterViewModel : BaseViewModel, IChapterRecord
    {
        private readonly IChapterRecord _chapterRecord;

        /// <summary>
        /// Download info. 
        /// </summary>
        private DownloadedChapterInfo _downloadInfo;

        public ChapterViewModel(IChapterRecord chapter, DownloadedChapterInfo downloadInfo)
        {
            if (chapter == null)
                throw new ArgumentNullException("chapter");

            _chapterRecord = chapter;
            _downloadInfo = downloadInfo;
        }

        #region Implementation of IChapterRecord interface

        public string ChapterId
        {
            get { return _chapterRecord.ChapterId; }
        }

        public string MangaName
        {
            get { return _chapterRecord.MangaName; }
        }

        public string ChapterName
        {
            get { return _chapterRecord.ChapterName; }
        }

        public string Url
        {
            get { return _chapterRecord.Url; }
        }

        public IMangaRecord MangaRecord
        {
            get { return _chapterRecord.MangaRecord; }
        }

        public Guid Scraper
        {
            get { return _chapterRecord.Scraper; }
        }

        #endregion // Implementation of IChapterRecord interface

        public DownloadedChapterInfo DownloadInfo
        {
            get { return _downloadInfo; }

            set
            {
                _downloadInfo = value;

                OnPropertyChanged(() => Downloaded);
            }
        }

        public bool Downloaded
        {
            get
            {
                return DownloadInfo != null;
            }
        }
    }
}

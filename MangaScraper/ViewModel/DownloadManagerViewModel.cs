using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Common.Utils;
using System.Collections.ObjectModel;
using Blacker.MangaScraper.Library;
using Blacker.MangaScraper.Library.Models;
using log4net;

namespace Blacker.MangaScraper.ViewModel
{
    /// <summary>
    /// fixme:: this needs to be fixed so it doesn't load all history
    /// </summary>
    class DownloadManagerViewModel : BaseViewModel
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (DownloadManagerViewModel));

        private readonly ISemaphore _downloadsSemaphore;
        private readonly LibraryManager _libraryManager;
        private readonly ListCollectionView _downloadsCollectionView;
        
        /// <summary>
        /// Index of the selected tab, initialized to -1 in order to properly initialize attached stuff later on.
        /// </summary>
        private int _selectedTabIndex = -1;

        public DownloadManagerViewModel()
        {
            _downloadsSemaphore = new FifoSemaphore(Properties.Settings.Default.MaxParallelDownloads);

            _libraryManager = new LibraryManager(ScraperLoader.Instance.AllScrapers);

            var olderDownloads = new List<DownloadViewModel>();

            foreach (DownloadedChapterInfo chapterInfo in _libraryManager.GetDownloads())
            {
                var downloadViewModel = new DownloadViewModel(chapterInfo, _downloadsSemaphore);
                
                downloadViewModel.RemoveFromCollection += DownloadViewModel_RemoveFromCollection;
                downloadViewModel.DownloadCompleted += DownloadViewModel_DownloadCompleted;

                olderDownloads.Add(downloadViewModel);
            }

            Downloads = new AsyncObservableCollection<DownloadViewModel>(olderDownloads);
            _downloadsCollectionView = (ListCollectionView) CollectionViewSource.GetDefaultView(Downloads);
            _downloadsCollectionView.CustomSort = new DownloadAgeComparer();

            // init filter
            SelectedTabIndex = Properties.Settings.Default.SelectedDownloadsTab;
        }

        public ObservableCollection<DownloadViewModel> Downloads { get; private set; }

        public ListCollectionView DownloadsCollectionView { get { return _downloadsCollectionView; } }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                if(_selectedTabIndex == value)
                    return;

                switch (value)
                {
                    case -1:    // no tab selected, do nothing
                        break;
                    case 0:     // last 7 days
                        _downloadsCollectionView.Filter = o => DownloadDateTimeFilter(DateTime.UtcNow - TimeSpan.FromDays(7), o);
                        break;
                    case 1:     // last month
                        _downloadsCollectionView.Filter = o => DownloadDateTimeFilter(DateTime.UtcNow.AddMonths(-1), o);
                        break;
                    case 2:     // this year
                        _downloadsCollectionView.Filter = o => DownloadDateTimeFilter(new DateTime(DateTime.Today.Year, 1, 1), o);
                        break;
                    case 3:     // all
                        _downloadsCollectionView.Filter = null;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown selected tab index.");
                }

                _selectedTabIndex = value;
                Properties.Settings.Default.SelectedDownloadsTab = _selectedTabIndex;
            }
        }

        public bool HasActiveDownloads
        {
            get { return Downloads.Any(dvm => !dvm.Completed); }
        }

        public void Download(IDownloader downloader, IChapterRecord chapter, string outputPath, bool zipFile)
        {
            var downloadViewModel = new DownloadViewModel(new DownloadedChapterInfo(chapter), _downloadsSemaphore);
            
            downloadViewModel.RemoveFromCollection += DownloadViewModel_RemoveFromCollection;
            downloadViewModel.DownloadCompleted += DownloadViewModel_DownloadCompleted;
            
            Downloads.Add(downloadViewModel);

            downloadViewModel.DownloadChapter(outputPath, zipFile);

            InvokePropertyChanged("HasActiveDownloads");
        }

        private void DownloadViewModel_RemoveFromCollection(object sender, EventArgs<DownloadedChapterInfo> eventArgs)
        {
            var downloadViewModel = sender as DownloadViewModel;

            downloadViewModel.RemoveFromCollection -= DownloadViewModel_RemoveFromCollection;

            Downloads.Remove(downloadViewModel);

            _libraryManager.RemoveDownloadInfo(eventArgs.Value);
        }

        private void DownloadViewModel_DownloadCompleted(object sender, EventArgs<DownloadedChapterInfo> eventArgs)
        {
            _libraryManager.StoreDownloadInfo(eventArgs.Value);
            _downloadsCollectionView.Refresh();

            InvokePropertyChanged("HasActiveDownloads");
        }

        public void CancelRunningDownloads()
        {
            foreach (var download in Downloads.Where(d => !d.Completed))
            {
                try
                {
                    download.Cancel(null);
                }
                catch (Exception ex)
                {
                    _log.Error("Unable to cancel download.", ex);
                }
            }
        }

        private bool DownloadDateTimeFilter(DateTime limit, object o)
        {
            var dvm = o as DownloadViewModel;

            if (dvm == null)
                return false;

            // show unfinished downloads no matter what the limit is
            if (dvm.Downloaded == DateTime.MinValue)
                return true;

            return dvm.Downloaded >= limit;
        }

        private class DownloadAgeComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                var objX = x as DownloadViewModel;
                var objY = y as DownloadViewModel;

                if (objX == null)
                    throw new ArgumentException("DownloadAgeComparer can only sort non null objects of type DownloadViewModel", "x");
                if (objY == null)
                    throw new ArgumentException("DownloadAgeComparer can only sort non null objects of type DownloadViewModel", "y");

                if (objX.Downloaded == objY.Downloaded)
                    return 0;

                if (objX.Downloaded == DateTime.MinValue)
                    return -1;

                if (objY.Downloaded == DateTime.MinValue)
                    return 1;

                if (objX.Downloaded < objY.Downloaded)
                    return 1;

                return -1;
            }
        }
    }
}

using System;
using System.Linq;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Common.Utils;
using System.Collections.ObjectModel;
using log4net;

namespace Blacker.MangaScraper.ViewModel
{
    class DownloadManagerViewModel : BaseViewModel
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (DownloadManagerViewModel));
        private readonly ISemaphore _downloadsSemaphore;

        public DownloadManagerViewModel()
        {
            _downloadsSemaphore = new FifoSemaphore(Properties.Settings.Default.MaxParallelDownloads);

            Downloads = new AsyncObservableCollection<DownloadViewModel>();
        }

        public ObservableCollection<DownloadViewModel> Downloads { get; private set; }

        public void Download(IDownloader downloader, IChapterRecord chapter, string outputPath, bool zipFile)
        {
            var downloadViewModel = new DownloadViewModel(downloader, chapter);
            downloadViewModel.RemoveFromCollection += DownloadViewModel_RemoveFromCollection;
            Downloads.Add(downloadViewModel);

            downloadViewModel.DownloadChapter(outputPath, zipFile, _downloadsSemaphore);
        }

        void DownloadViewModel_RemoveFromCollection(object sender, EventArgs e)
        {
            var downloadViewModel = sender as DownloadViewModel;

            downloadViewModel.RemoveFromCollection -= DownloadViewModel_RemoveFromCollection;

            Downloads.Remove(downloadViewModel);
        }

        public void CancelRunningDownloads()
        {
            foreach (var download in Downloads.Where(d => !d.Completed))
            {
                try
                {
                    download.Cancel();
                }
                catch (Exception ex)
                {
                    _log.Error("Unable to cancel download.", ex);
                }
            }
        }
    }
}

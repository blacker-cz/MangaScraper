using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.Scraper;
using System.Windows.Input;
using Blacker.MangaScraper.Commands;
using Blacker.Scraper.Models;

namespace Blacker.MangaScraper.ViewModel
{
    class DownloadViewModel : BaseViewModel
    {
        private readonly IDownloader _downloader;
        private readonly ChapterRecord _chapter;

        private readonly ICommand _cancelDownloadCommand;
        private readonly ICommand _removeDownloadCommand;

        private DownloadState _downloadState;

        public DownloadViewModel(IDownloader downloader, ChapterRecord chapter)
            : base()
        {
            if (downloader == null)
                throw new ArgumentNullException("downloader");
            if (chapter == null)
                throw new ArgumentNullException("chapter");

            _downloader = downloader;
            _chapter = chapter;

            // register downloader events
            _downloader.DownloadProgress += _downloader_DownloadProgress;
            _downloader.DownloadCompleted += _downloader_DownloadCompleted;

            _cancelDownloadCommand = new CancelDownloadCommand(this);
            _removeDownloadCommand = new RemoveDownloadCommand(this);

            State = DownloadState.Ok;
            Completed = false;
        }

        public event EventHandler DownloadCompleted;
        public event EventHandler RemoveFromCollection;

        public ICommand CancelDownloadCommand { get { return _cancelDownloadCommand; } }
        public ICommand RemoveDownloadCommand { get { return _removeDownloadCommand; } }

        public int ProgressValue { get; set; }

        public ChapterRecord Chapter { get { return _chapter; } }

        public IDownloader Downloader { get { return _downloader; } }

        public string CurrentActionText { get; set; }

        public bool Completed { get; private set; }

        private DownloadState State 
        {
            get { return _downloadState; }
            set
            {
                _downloadState = value;
                InvokePropertyChanged("StateColor");
            } 
        }

        public string StateColor
        {
            get
            {
                switch (State)
                {
                    case DownloadState.Ok:
                        return "HoneyDew";
                    case DownloadState.Warning:
                        return "LemonChiffon";
                    case DownloadState.Error:
                        return "LightSalmon";
                    default:
                        return "LightSalmon";
                }
            }
        }

        public void Cancel()
        {
            _downloader.Cancel();
        }

        public void Remove()
        {
            _downloader.Cancel();

            OnRemoveFromCollection();
        }

        void _downloader_DownloadProgress(object sender, Scraper.Events.DownloadProgressEventArgs e)
        {
            ProgressValue = e.PercentComplete;
            CurrentActionText = e.Message;

            InvokePropertyChanged("ProgressValue");
            InvokePropertyChanged("CurrentActionText");
        }

        void _downloader_DownloadCompleted(object sender, Scraper.Events.DownloadCompletedEventArgs e)
        {
            State = DownloadState.Ok;

            if (e.Cancelled)
            {
                CurrentActionText = "Download was cancelled.";
                State = DownloadState.Warning;
            }
            else if (e.Error != null)
            {
                CurrentActionText = "Unable to download/save requested chaper";
                State = DownloadState.Error;
            }

            InvokePropertyChanged("ProgressValue");
            InvokePropertyChanged("CurrentActionText");

            Completed = true;

            OnDownloadCompleted();
        }

        private void OnDownloadCompleted()
        {
            if (DownloadCompleted != null)
            {
                DownloadCompleted(this, null);
            }
        }

        private void OnRemoveFromCollection()
        {
            if (RemoveFromCollection != null)
            {
                RemoveFromCollection(this, null);
            }
        }

        private enum DownloadState
        {
            Ok,
            Warning,
            Error
        }
    }
}

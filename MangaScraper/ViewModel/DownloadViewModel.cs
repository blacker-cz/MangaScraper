using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.Scraper;
using System.Windows.Input;
using Blacker.MangaScraper.Commands;
using Blacker.Scraper.Models;
using System.IO;
using System.Text.RegularExpressions;
using log4net;

namespace Blacker.MangaScraper.ViewModel
{
    class DownloadViewModel : BaseViewModel
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DownloadViewModel));

        private readonly IDownloader _downloader;
        private readonly ChapterRecord _chapter;

        private readonly ICommand _cancelDownloadCommand;
        private readonly ICommand _removeDownloadCommand;
        private readonly ICommand _openDownloadCommand;

        private DownloadState _downloadState;

        private bool _isZipped;
        private string _outputFullPath;

        private static readonly Regex _invalidPathCharsRegex = new Regex(string.Format("[{0}]",
                                    Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))),
                               RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private const string ButtonCancelText = @"Cancel";
        private const string ButtonCancellingText = @"Canceling";

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
            _openDownloadCommand = new OpenDownloadCommand(this);

            State = DownloadState.Ok;
            Completed = false;
            CancelText = ButtonCancelText;
        }

        public event EventHandler DownloadCompleted;
        public event EventHandler RemoveFromCollection;

        public ICommand CancelDownloadCommand { get { return _cancelDownloadCommand; } }
        public ICommand RemoveDownloadCommand { get { return _removeDownloadCommand; } }
        public ICommand OpenDownloadCommand { get { return _openDownloadCommand; } }

        public string CancelText { get; set; }

        public int ProgressValue { get; set; }

        public ChapterRecord Chapter { get { return _chapter; } }

        public IDownloader Downloader { get { return _downloader; } }

        public string CurrentActionText { get; set; }

        public bool Completed { get; private set; }

        public bool CanOpen { get { return Completed && State == DownloadState.Ok; } }

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

        public void DownloadChapter(string outputPath, bool isZipped)
        {
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Invalid output path", "outputPath");

            _isZipped = isZipped;

            if (isZipped)
            {
                var fileInfo = new FileInfo(Path.Combine(outputPath, GetNameForSave(Chapter) + ".zip"));
                _outputFullPath = fileInfo.FullName;
                Downloader.DownloadChapterAsync(Chapter, fileInfo);
            }
            else
            {
                var directoryInfo = new DirectoryInfo(Path.Combine(outputPath, GetNameForSave(Chapter)));
                _outputFullPath = directoryInfo.FullName;
                Downloader.DownloadChapterAsync(Chapter, directoryInfo);
            }
        }

        public void Open()
        {
            if (!Completed && State != DownloadState.Ok)
                throw new InvalidOperationException();

            try
            {
                if (_isZipped)
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.ReaderPath))
                        System.Diagnostics.Process.Start(Properties.Settings.Default.ReaderPath, _outputFullPath);
                    else
                        System.Diagnostics.Process.Start(_outputFullPath);
                }
                else
                {
                    System.Diagnostics.Process.Start(_outputFullPath);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Unable to open downloaded chapter.", ex);
            }
        }

        public void Cancel()
        {
            _downloader.Cancel();
            
            CancelText = ButtonCancellingText;
            InvokePropertyChanged("CancelText");
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

            Completed = true;

            InvokePropertyChanged("ProgressValue");
            InvokePropertyChanged("CurrentActionText");
            InvokePropertyChanged("Completed");
            InvokePropertyChanged("CanOpen");

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

        private string GetNameForSave(ChapterRecord chapter)
        {
            string fileName = String.Format("{0} - {1}", chapter.MangaName, chapter.ChapterName).Replace(" ", "_");

            return _invalidPathCharsRegex.Replace(fileName, "");
        }

    }
}

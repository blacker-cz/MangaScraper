using System;
using System.Collections.Generic;
using System.Linq;
using Blacker.Scraper;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Blacker.MangaScraper.Commands;
using Blacker.Scraper.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Blacker.MangaScraper.Helpers;

namespace Blacker.MangaScraper
{
    class MainWindowViewModel : BaseViewModel
    {
        private readonly IList<IScraper> _scrapers;

        private IScraper _currentScraper;

        private readonly ICommand _searchCommand;
        private readonly ICommand _browseCommand;
        private readonly ICommand _saveCommand;

        private MangaRecord _selectedManga;

        private string _outputPath;
        private string _searchString = string.Empty;
        private readonly BackgroundWorker _downloadWorker;

        private static readonly Regex _invalidPathCharsRegex = new Regex(string.Format("[{0}]",
                                    Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))),
                               RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public MainWindowViewModel()
        {
            _searchCommand = new SearchCommand(this);
            _browseCommand = new BrowseCommand(this);
            _saveCommand = new SaveCommand(this);

            _scrapers = new List<IScraper>(ReflectionHelper.GetInstances<IScraper>());

            if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedScraper))
                CurrentScraper = _scrapers.FirstOrDefault(s => s.Name == Properties.Settings.Default.SelectedScraper);

            if (CurrentScraper == null)
                CurrentScraper = _scrapers.First();

            // load output path from user settings
            _outputPath = Properties.Settings.Default.OutputPath;

            _downloadWorker = new BackgroundWorker();
            _downloadWorker.DoWork += new DoWorkEventHandler(_downloadWorker_DoWork);
            _downloadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_downloadWorker_RunWorkerCompleted);

            Mangas = new AsyncObservableCollection<MangaRecord>();
            Chapters = new AsyncObservableCollection<ChapterRecord>();

            ZipFile = true;
            ProgressMax = 1;
            ProgressValue = 0;
        }

        public IList<IScraper> Scrapers { get { return _scrapers; } }

        public IScraper CurrentScraper
        {
            get { return _currentScraper; }
            set
            {
                if (_currentScraper != null)
                    _currentScraper.DownloadProgress -= CurrentScraper_DownloadProgress;

                _currentScraper = value;
                _currentScraper.DownloadProgress += CurrentScraper_DownloadProgress;

                // remember selected scraper
                Properties.Settings.Default.SelectedScraper = _currentScraper.Name;
                Properties.Settings.Default.Save();
            }
        }

        public string SearchString
        {
            get { return _searchString; }
            set
            {
                _searchString = value;
                SearchMangaImmediate(_searchString);
            }
        }

        public ICommand SearchCommand { get { return _searchCommand; } }

        public ICommand BrowseCommand { get { return _browseCommand; } }

        public ICommand SaveCommand { get { return _saveCommand; } }

        public ObservableCollection<MangaRecord> Mangas { get; private set; }

        public ObservableCollection<ChapterRecord> Chapters { get; private set; }

        public string OutputPath
        {
            get { return _outputPath; }
            set
            {
                _outputPath = value;
                // remember path in user settings
                Properties.Settings.Default.OutputPath = _outputPath;
                Properties.Settings.Default.Save();
            }
        }

        public bool ZipFile { get; set; }

        public string CurrentActionText { get; set; }

        public int ProgressValue { get; set; }

        public int ProgressMax { get; set; }

        public bool ProgresIndeterminate { get; set; }

        public MangaRecord SelectedManga
        {
            get { return _selectedManga; }
            set
            {
                _selectedManga = value;
                LoadChapters(_selectedManga);
            }
        }

        public ChapterRecord SelectedChapter { get; set; }

        #region Commands

        public void SearchManga()
        {
            // todo: call should be done in different thread
            Mangas.Clear();
            foreach (var item in CurrentScraper.GetAvailableMangas(SearchString ?? String.Empty))
            {
                Mangas.Add(item);
            }
        }

        private void SearchMangaImmediate(string filter)
        {
            if (!(CurrentScraper is IImmediateSearchProvider) || filter == null)
                return;

            // todo: should this be also called in different thread?
            Mangas.Clear();
            foreach (var item in (CurrentScraper as IImmediateSearchProvider).GetAvailableMangasImmediate(filter))
            {
                Mangas.Add(item);
            }
        }

        private void LoadChapters(MangaRecord manga)
        {
            if (manga == null)
                return;

            // todo: call should be done in different thread
            Chapters.Clear();
            foreach (var item in CurrentScraper.GetAvailableChapters(manga))
            {
                Chapters.Add(item);
            }
        }

        /// <summary>
        /// Browse for output folder
        /// </summary>
        public void BrowseClicked()
        {
            // WPF doesn't have folder browser dialog, so we have to use the one from Windows.Forms
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.SelectedPath = OutputPath;
                dlg.Description = "Select output directory.";
                dlg.ShowNewFolderButton = true;

                System.Windows.Forms.DialogResult result = dlg.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    OutputPath = dlg.SelectedPath;
                    InvokePropertyChanged("OutputPath");
                }
            }
        }

        public void SaveChapter()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                MessageBox.Show("Output path must be selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SelectedChapter == null)
            {
                MessageBox.Show("Chapter must be selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var workerParams = new WorkerParams()
            {
                Scraper = CurrentScraper,
                Chapter = SelectedChapter,
                IsFile = ZipFile
            };

            if (ZipFile)
                workerParams.File = new FileInfo(Path.Combine(OutputPath, GetNameForSave(SelectedChapter) + ".zip"));
            else
                workerParams.Directory = new DirectoryInfo(Path.Combine(OutputPath, GetNameForSave(SelectedChapter)));

            ((BaseCommand)SaveCommand).Disabled = true;
            InvokePropertyChanged("SaveCommand");

            _downloadWorker.RunWorkerAsync(workerParams);
        }

        #endregion // Commands

        private string GetNameForSave(ChapterRecord chapter)
        {
            string fileName = String.Format("{0} - {1}", chapter.MangaName, chapter.ChapterName).Replace(" ", "_");
            
            return _invalidPathCharsRegex.Replace(fileName, "");
        }

        void CurrentScraper_DownloadProgress(object sender, Scraper.Events.DownloadProgressEventArgs e)
        {
            ProgressValue = e.Done;
            ProgressMax = e.From;
            CurrentActionText = e.Action;

            InvokePropertyChanged("ProgressValue");
            InvokePropertyChanged("ProgressMax");
            InvokePropertyChanged("CurrentActionText");
        }

        void _downloadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressValue = 0;
            ProgressMax = 0;

            ((BaseCommand)SaveCommand).Disabled = false;

            InvokePropertyChanged("ProgressValue");
            InvokePropertyChanged("ProgressMax");
            InvokePropertyChanged("SaveCommand");

            if (e.Error != null)
            {
                MessageBox.Show("Unable to download/save requested chaper, error reason is:\n\n\"" + e.Error.Message + "\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void _downloadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var workerParams = e.Argument as WorkerParams;

            if (workerParams.IsFile)
                workerParams.Scraper.DownloadChapter(workerParams.Chapter, workerParams.File);
            else
                workerParams.Scraper.DownloadChapter(workerParams.Chapter, workerParams.Directory);
        }

        private class WorkerParams
        {
            public IScraper Scraper { get; set; }
            public ChapterRecord Chapter { get; set; }
            public bool IsFile { get; set; }
            public FileInfo File { get; set; }
            public DirectoryInfo Directory { get; set; }
        }
    }
}

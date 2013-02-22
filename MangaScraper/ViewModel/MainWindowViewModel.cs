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
using log4net;
using Blacker.Scraper.Utils;

namespace Blacker.MangaScraper.ViewModel
{
    class MainWindowViewModel : BaseViewModel, ICleanup, IBrowseCommand, ISaveCommand
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindowViewModel));

        private readonly IEnumerable<IScraper> _scrapers;

        private IScraper _currentScraper;

        private AsyncRequestQueue _requestQueue;

        private readonly ICommand _searchCommand;
        private readonly ICommand _browseCommand;
        private readonly ICommand _saveCommand;

        private MangaRecord _selectedManga;

        private string _outputPath;
        private string _searchString = string.Empty;

        private static readonly object _syncRoot = new object();

        private readonly DownloadManagerViewModel _downloadManager;

        public MainWindowViewModel(Window owner)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");

            Owner = owner;

            _searchCommand = new SearchCommand(this);
            _browseCommand = new BrowseCommand(this);
            _saveCommand = new SaveCommand(this);

            // load all enabled scrapers
            _scrapers = ScraperLoader.Instance.EnabledScrapers;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedScraper))
                CurrentScraper = _scrapers.FirstOrDefault(s => s.Name == Properties.Settings.Default.SelectedScraper);

            if (CurrentScraper == null)
                CurrentScraper = _scrapers.First();

            // load output path from user settings
            _outputPath = Properties.Settings.Default.OutputPath;

            Mangas = new AsyncObservableCollection<MangaRecord>();
            Chapters = new AsyncObservableCollection<ChapterRecord>();
            SelectedChapters = new AsyncObservableCollection<ChapterRecord>();

            ZipFile = true;

            _requestQueue = new AsyncRequestQueue(System.Threading.SynchronizationContext.Current);
            _requestQueue.TasksCompleted += _requestQueue_TasksCompleted;
            _requestQueue.Initialize();

            _downloadManager = new DownloadManagerViewModel();

            if (Properties.Settings.Default.EnablePreload)
            {
                PreloadMangas();
            }
        }

        public IEnumerable<IScraper> Scrapers { get { return _scrapers; } }

        public IScraper CurrentScraper
        {
            get { return _currentScraper; }
            set
            {
                _currentScraper = value;

                // remember selected scraper
                Properties.Settings.Default.SelectedScraper = _currentScraper.Name;
                Properties.Settings.Default.Save();

                // clear lists
                if(Mangas != null)
                    Mangas.Clear();
                if(Chapters != null)
                    Chapters.Clear();
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

        public IEnumerable<Models.RecentItem> RecentFolders { get { return Properties.Settings.Default.RecentFolders; } }

        public ICommand SearchCommand { get { return _searchCommand; } }

        public ICommand BrowseCommand { get { return _browseCommand; } }

        public ICommand SaveCommand { get { return _saveCommand; } }

        public ObservableCollection<MangaRecord> Mangas { get; private set; }

        public ObservableCollection<ChapterRecord> Chapters { get; private set; }

        public ObservableCollection<ChapterRecord> SelectedChapters { get; private set; }

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

        public bool OperationInProgress { get; set; }

        public MangaRecord SelectedManga
        {
            get { return _selectedManga; }
            set
            {
                _selectedManga = value;
                LoadChapters(_selectedManga);
            }
        }

        public DownloadManagerViewModel DownloadManager { get { return _downloadManager; } }

        #region Commands

        public void SearchManga()
        {
            var scraper = CurrentScraper;
            var searchString = SearchString ?? String.Empty;

            OperationInProgress = true;
            InvokePropertyChanged("OperationInProgress");
            CurrentActionText = "Searching ...";
            InvokePropertyChanged("CurrentActionText");

            _requestQueue.Add(
                () => {
                    return scraper.GetAvailableMangas(searchString);
                },
                (r, e) => {
                    var records = r as IEnumerable<MangaRecord>;
                    if (e == null && r != null)
                    {
                        lock (_syncRoot)
                        {
                            // just replace collection -> this is easier than removing and than adding records
                            Mangas = new AsyncObservableCollection<MangaRecord>(records);
                            InvokePropertyChanged("Mangas");
                        }
                    }
                }
            );
        }

        private void SearchMangaImmediate(string filter)
        {
            if (!(CurrentScraper is IImmediateSearchProvider) || filter == null)
                return;

            var scraper = CurrentScraper as IImmediateSearchProvider;
            var searchString = SearchString ?? String.Empty;

            OperationInProgress = true;
            InvokePropertyChanged("OperationInProgress");
            CurrentActionText = "Searching ...";
            InvokePropertyChanged("CurrentActionText");

            _requestQueue.Add(
                () =>
                {
                    return scraper.GetAvailableMangasImmediate(searchString);
                },
                (r, e) =>
                {
                    var requests = r as IEnumerable<MangaRecord>;
                    if (e == null && r != null)
                    {
                        lock (_syncRoot)
                        {
                            Mangas = new AsyncObservableCollection<MangaRecord>(requests);
                            InvokePropertyChanged("Mangas");
                        }
                    }
                }
            );
        }

        private void LoadChapters(MangaRecord manga)
        {
            if (manga == null)
                return;

            var scraper = CurrentScraper;

            OperationInProgress = true;
            InvokePropertyChanged("OperationInProgress");
            CurrentActionText = "Loading chapters ...";
            InvokePropertyChanged("CurrentActionText");

            _requestQueue.Add(
                () =>
                {
                    return scraper.GetAvailableChapters(manga);
                },
                (r, e) =>
                {
                    var results = r as IEnumerable<ChapterRecord>;
                    if (e == null && r != null)
                    {
                        lock (_syncRoot)
                        {
                            // just replace collection -> this is easier than removing and than adding records
                            Chapters = new AsyncObservableCollection<ChapterRecord>(results);
                            InvokePropertyChanged("Chapters");
                        }
                    }
                }
            );
        }

        /// <summary>
        /// Browse for output folder
        /// </summary>
        public void BrowseClicked(object parameter)
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

        public void SaveClicked(object parameter)
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                MessageBox.Show("Output folder must be selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SelectedChapters.Count == 0)
            {
                MessageBox.Show("Chapter must be selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!Directory.Exists(OutputPath))
            {
                if (MessageBox.Show("The output folder doesn't exist. Would you like to create it?", "Output folder",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(OutputPath);
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Unable to create output folder.", ex);
                        MessageBox.Show("Unable to create output folder", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    // if user don't want us to create the output folder, we will simply don't start the download
                    return;
                }
            }

            // save output path to recent list
            Properties.Settings.Default.RecentFolders.Add(OutputPath);
            InvokePropertyChanged("RecentFolders");

            foreach (var selectedChapter in SelectedChapters)
            {
                _downloadManager.Download(CurrentScraper.GetDownloader(), selectedChapter, OutputPath, ZipFile);
            }
        }

        #endregion // Commands

        private void PreloadMangas()
        {
            var preloadables = _scrapers.Where(s => s is IPreload).Select(s => s as IPreload);

            // if there are no preloadables skip the execution
            if (!preloadables.Any())
                return;

            if (!OperationInProgress)
            {
                CurrentActionText = "Preloading manga directories ...";
                OperationInProgress = true;
                InvokePropertyChanged("OperationInProgress");
                InvokePropertyChanged("CurrentActionText");
            }

            var async = new AsyncWrapper();
            async.Call<object>(() =>
                                {
                                    System.Threading.Tasks.Parallel.ForEach(preloadables, (x) => { x.PreloadDirectory(); });
                                    return null;
                                },
                                (x, y) =>
                                {
                                    // don't care about errors...
                                    OperationInProgress = false;
                                    InvokePropertyChanged("OperationInProgress");
                                    CurrentActionText = "";
                                    InvokePropertyChanged("CurrentActionText");
                                }
                        );
        }

        void _requestQueue_TasksCompleted(object sender, EventArgs e)
        {
            OperationInProgress = false;
            InvokePropertyChanged("OperationInProgress");
            CurrentActionText = "";
            InvokePropertyChanged("CurrentActionText");
        }

        #region ICleanup implementation

        public void Cleanup()
        {
            if (_requestQueue != null)
                _requestQueue.Stop();

            try
            {
                _downloadManager.CancelRunningDownloads();
            }
            catch (Exception ex)
            {
                _log.Error("Unable to cancel download during cleanup.", ex);
            }
        }

        #endregion
    }
}

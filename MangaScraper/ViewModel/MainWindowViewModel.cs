using System;
using System.Collections.Generic;
using System.Linq;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Blacker.MangaScraper.Commands;
using System.Collections.ObjectModel;
using Blacker.MangaScraper.Helpers;
using Blacker.MangaScraper.Services;
using log4net;

namespace Blacker.MangaScraper.ViewModel
{
    class MainWindowViewModel : BaseViewModel, ICleanup
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindowViewModel));

        private readonly IEnumerable<IScraper> _scrapers;

        private IScraper _currentScraper;

        private readonly AsyncRequestQueue _requestQueue;

        private readonly RelayCommand _searchCommand;
        private readonly RelayCommand _browseCommand;
        private readonly RelayCommand _saveCommand;

        private IMangaRecord _selectedManga;

        private string _outputPath;
        private string _searchString = string.Empty;

        private static readonly object _syncRoot = new object();

        private readonly DownloadManagerViewModel _downloadManager;
        private bool _operationInProgress;
        private string _currentActionText;

        public MainWindowViewModel(Window owner)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");

            Owner = owner;

            _searchCommand = new RelayCommand(SearchManga);
            _browseCommand = new RelayCommand(BrowseClicked);
            _saveCommand = new RelayCommand(SaveClicked);

            // load all enabled scrapers
            _scrapers = ScraperLoader.Instance.EnabledScrapers;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedScraper))
                CurrentScraper = _scrapers.FirstOrDefault(s => s.Name == Properties.Settings.Default.SelectedScraper);

            if (CurrentScraper == null)
                CurrentScraper = _scrapers.First();

            // load output path from user settings
            _outputPath = Properties.Settings.Default.OutputPath;

            Mangas = new AsyncObservableCollection<IMangaRecord>();
            Chapters = new AsyncObservableCollection<IChapterRecord>();
            SelectedChapters = new AsyncObservableCollection<IChapterRecord>();

            ZipFile = true;

            _requestQueue = new AsyncRequestQueue();
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

        public ObservableCollection<IMangaRecord> Mangas { get; private set; }

        public ObservableCollection<IChapterRecord> Chapters { get; private set; }

        public ObservableCollection<IChapterRecord> SelectedChapters { get; private set; }

        public string OutputPath
        {
            get { return _outputPath; }
            set
            {
                _outputPath = value;
                // remember path in user settings
                Properties.Settings.Default.OutputPath = _outputPath;
                Properties.Settings.Default.Save();

                InvokePropertyChanged("OutputPath");
            }
        }

        public bool ZipFile { get; set; }

        public string CurrentActionText
        {
            get { return _currentActionText; }
            set
            {
                _currentActionText = value;
                InvokePropertyChanged("CurrentActionText");
            }
        }

        public bool OperationInProgress
        {
            get { return _operationInProgress; }
            set
            {
                _operationInProgress = value;
                InvokePropertyChanged("OperationInProgress");
            }
        }

        public IMangaRecord SelectedManga
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

        public void SearchManga(object parameter)
        {
            var scraper = CurrentScraper;
            var searchString = SearchString ?? String.Empty;

            OperationInProgress = true;
            CurrentActionText = "Searching ...";

            _requestQueue.Add(
                () => scraper.GetAvailableMangas(searchString),
                (r, e) => {
                    var records = r as IEnumerable<IMangaRecord>;
                    if (e == null && r != null)
                    {
                        lock (_syncRoot)
                        {
                            // just replace collection -> this is easier than removing and than adding records
                            Mangas = new AsyncObservableCollection<IMangaRecord>(records);
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
            CurrentActionText = "Searching ...";

            _requestQueue.Add(
                () => scraper.GetAvailableMangasImmediate(searchString),
                (r, e) =>
                {
                    var requests = r as IEnumerable<IMangaRecord>;
                    if (e == null && r != null)
                    {
                        lock (_syncRoot)
                        {
                            Mangas = new AsyncObservableCollection<IMangaRecord>(requests);
                            InvokePropertyChanged("Mangas");
                        }
                    }
                }
            );
        }

        private void LoadChapters(IMangaRecord manga)
        {
            if (manga == null)
                return;

            var scraper = CurrentScraper;

            OperationInProgress = true;
            CurrentActionText = "Loading chapters ...";

            _requestQueue.Add(
                () => scraper.GetAvailableChapters(manga),
                (r, e) =>
                {
                    var results = r as IEnumerable<IChapterRecord>;
                    if (e == null && r != null)
                    {
                        lock (_syncRoot)
                        {
                            // just replace collection -> this is easier than removing and than adding records
                            Chapters = new AsyncObservableCollection<IChapterRecord>(results);
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
            ServiceLocator.Instance.GetService<IInteractionService>()
                          .ShowFolderBrowserDialog(OutputPath,
                                                   "Select output directory",
                                                   true,
                                                   (result, path) =>
                                                       {
                                                           if (result == System.Windows.Forms.DialogResult.OK)
                                                           {
                                                               OutputPath = path;
                                                           }
                                                       });
        }

        public void SaveClicked(object parameter)
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                ServiceLocator.Instance.GetService<IInteractionService>().ShowError("Output folder must be selected.");
                return;
            }

            if (SelectedChapters.Count == 0)
            {
                ServiceLocator.Instance.GetService<IInteractionService>().ShowError("Chapter must be selected.");
                return;
            }

            if (!Directory.Exists(OutputPath))
            {
                if (ServiceLocator.Instance.GetService<IInteractionService>()
                                  .ShowMessageBox("The output folder doesn't exist. Would you like to create it?",
                                                  "Output folder",
                                                  MessageBoxButton.YesNo,
                                                  MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(OutputPath);
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Unable to create output folder.", ex);
                        ServiceLocator.Instance.GetService<IInteractionService>().ShowError("Unable to create output folder");
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
            var preloadables = _scrapers.OfType<IPreload>();

            // if there are no preloadables skip the execution
            if (!preloadables.Any())
                return;

            if (!OperationInProgress)
            {
                CurrentActionText = "Preloading manga directories ...";
                OperationInProgress = true;
            }

            AsyncWrapper.Call<object>(() =>
                                {
                                    System.Threading.Tasks.Parallel.ForEach(preloadables, (x) => x.PreloadDirectory());
                                    return null;
                                },
                                (x, y) =>
                                {
                                    // don't care about errors...
                                    OperationInProgress = false;
                                    CurrentActionText = "";
                                }
                        );
        }

        void _requestQueue_TasksCompleted(object sender, EventArgs e)
        {
            OperationInProgress = false;
            CurrentActionText = "";
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

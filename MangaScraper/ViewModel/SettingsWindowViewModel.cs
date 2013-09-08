using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Blacker.MangaScraper.Commands;
using System.ComponentModel;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Services;
using log4net;

namespace Blacker.MangaScraper.ViewModel
{
    class SettingsWindowViewModel : BaseViewModel, IDataErrorInfo
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingsWindowViewModel));

        private readonly RelayCommand _saveSettingsCommand;
        private readonly RelayCommand _browseCommand;
        private readonly RelayCommand _clearCommand;
        private string _readerPath;
        private string _chaptersSelectionMode;

        public SettingsWindowViewModel()
        {
            _saveSettingsCommand = new RelayCommand(SaveClicked);
            _browseCommand = new RelayCommand(BrowseClicked);
            _clearCommand = new RelayCommand(ClearClicked);


            MaxParallelDownloads = Properties.Settings.Default.MaxParallelDownloads;
            ReaderPath = Properties.Settings.Default.ReaderPath;
            EnablePreload = Properties.Settings.Default.EnablePreload;
            MaxRecentFolders = Properties.Settings.Default.RecentFolders.MaxItems;
            PreselectDownloadFolder = Properties.Settings.Default.PreselectOutputFolder;
            RecentMangaDaysNum = Properties.Settings.Default.RecentMangaDaysNum;
            ChaptersSelectionMode = Properties.Settings.Default.ChaptersSelectionMode;

            Scrapers = ScraperLoader.Instance.AllScrapers
                .Select(s =>
                    new ScraperInfo(s, !Properties.Settings.Default.DisabledScrapers.Contains(s.ScraperGuid))).ToList();

            try
            {
                using (var reader = new System.IO.StreamReader(System.Windows.Application.GetResourceStream(new System.Uri("/readme.txt", UriKind.Relative)).Stream, Encoding.UTF8))
                {
                    AboutText = reader.ReadToEnd();
                }
            }
            catch (System.IO.IOException ex)
            {
                _log.Error("Unable to load about information from resource.", ex);
                AboutText = "";
            }

        }

        public ICommand SaveSettingsCommand { get { return _saveSettingsCommand; } }

        public ICommand BrowseCommand { get { return _browseCommand; } }

        public ICommand ClearCommand { get { return _clearCommand; } }

        public string AboutText { get; set; }

        public ushort MaxParallelDownloads { get; set; }

        public string ReaderPath
        {
            get { return _readerPath; }
            set
            {
                _readerPath = value;
                InvokePropertyChanged("ReaderPath");
            }
        }

        public bool EnablePreload { get; set; }

        public uint MaxRecentFolders { get; set; }

        public bool PreselectDownloadFolder { get; set; }

        public int RecentMangaDaysNum { get; set; }

        public string ChaptersSelectionMode
        {
            get
            {
                return _chaptersSelectionMode;
            }
            set
            {
                _chaptersSelectionMode = value;
                InvokePropertyChanged("ChaptersSelectionMode");
            }
        }

        public IEnumerable<ScraperInfo> Scrapers { get; private set; }

        #region Commands

        public void SaveClicked(object parameter)
        {
            if (string.IsNullOrEmpty(Error))
            {
                Properties.Settings.Default.MaxParallelDownloads = MaxParallelDownloads;
                Properties.Settings.Default.ReaderPath = ReaderPath;
                Properties.Settings.Default.EnablePreload = EnablePreload;
                Properties.Settings.Default.PreselectOutputFolder = PreselectDownloadFolder;
                Properties.Settings.Default.RecentMangaDaysNum = RecentMangaDaysNum;
                Properties.Settings.Default.ChaptersSelectionMode = ChaptersSelectionMode;

                Properties.Settings.Default.RecentFolders.MaxItems = MaxRecentFolders;

                Properties.Settings.Default.DisabledScrapers.Clear();
                foreach (var scraper in Scrapers)
                {
                    if (!scraper.Enabled)
                        Properties.Settings.Default.DisabledScrapers.Add(scraper.ScraperGuid);
                }

                Properties.Settings.Default.Save();

                // todo: somehow signalize that settings were saved
            }
        }

        public void BrowseClicked(object parameter)
        {
            ServiceLocator.Instance.GetService<IInteractionService>()
                          .ShowOpenFileDialog(".exe", "Executables (.exe,.cmd,.bat)|*.exe;*.cmd;*.bat",
                                              (result, path) =>
                                                  {
                                                      if (result == DialogResult.OK)
                                                      {
                                                          ReaderPath = path;
                                                      }
                                                  });
        }

        public void ClearClicked(object parameter)
        {
            ServiceLocator.Instance.GetService<IInteractionService>()
                          .ShowMessageBox("Do you really want to clear recent folder history?",
                                          "Confirm action",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question,
                                          (result) =>
                                              {
                                                  if (result == MessageBoxResult.Yes)
                                                      Properties.Settings.Default.RecentFolders.Clear();
                                              });
        }

        #endregion // Commands

        #region IDataErrorInfo implementation

        public string Error
        {
            get { return this[string.Empty]; }
        }

        public string this[string columnName]
        {
            get
            {
                columnName = columnName ?? string.Empty;
                if (columnName == string.Empty || columnName == "MaxParallelDownloads")
                {
                    if (MaxParallelDownloads == 0 || MaxParallelDownloads > 50)
                    {
                        return "Maximum number of parallel downloads must be number from 1 to 50.";
                    }
                }
                if (columnName == string.Empty || columnName == "MaxRecentFolders")
                {
                    if (MaxRecentFolders > 50)
                    {
                        return "Maximum number of recent output folders must be number lower or equal to 50.";
                    }
                }
                if (columnName == string.Empty || columnName == "ReaderPath")
                {
                    if (!string.IsNullOrEmpty(ReaderPath) && !System.IO.File.Exists(ReaderPath))
                    {
                        return "File not found on the specified path.";
                    }
                }

                return string.Empty;
            }
        }

        #endregion // IDataErrorInfo implementation

        #region Scraper wrapper class

        public class ScraperInfo
        {
            private readonly IScraper _scraper;

            public ScraperInfo(IScraper scraper, bool enabled)
            {
                if (scraper == null)
                    throw new ArgumentNullException("scraper");

                _scraper = scraper;
                Enabled = enabled;
            }

            public string Name { get { return _scraper.Name; } }

            public Guid ScraperGuid { get { return _scraper.ScraperGuid; } }

            public bool Enabled { get; set; }
        }

        #endregion
    }
}

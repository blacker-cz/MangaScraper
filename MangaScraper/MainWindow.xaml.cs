using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace Blacker.MangaScraper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new ViewModel.MainWindowViewModel();

            this.Closed += MainWindow_Closed;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs cancelEventArgs)
        {
            var viewModel = this.DataContext as ViewModel.MainWindowViewModel;
            
            if (viewModel == null)
                return; // this should not happen

            if (viewModel.DownloadManager.HasActiveDownloads)
            {
                if (MessageBox.Show("There are still unfinished downloads. Are you sure you want to quit?",
                                    "Confirm exit",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question) ==
                    MessageBoxResult.No)
                {
                    cancelEventArgs.Cancel = true;
                }
            }
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            var cleanup = this.DataContext as ICleanup;
            if (cleanup != null)
                cleanup.Cleanup();

            Properties.Settings.Default.Save();
        }
    }
}

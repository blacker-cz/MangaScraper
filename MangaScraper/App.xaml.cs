using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Blacker.MangaScraper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // define application exception handler
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);
        }

        void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //process exception
#if !DEBUG
            MessageBox.Show("Application encountered following unrecoverable error and will now shut down:\n\n\"" + e.Exception.Message + "\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;
            
            // kill application
            this.MainWindow.Close();
#endif
        }
    }
}

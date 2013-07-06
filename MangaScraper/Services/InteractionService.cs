using System;
using System.Windows;
using System.Windows.Forms;
using log4net;

namespace Blacker.MangaScraper.Services
{
    internal class InteractionService : IInteractionService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(InteractionService));

        public void ShowMessageBox(string text, Action<MessageBoxResult> callback)
        {
            ShowMessageBox(text, String.Empty, callback);
        }

        public void ShowMessageBox(string text, string caption, Action<MessageBoxResult> callback)
        {
            ShowMessageBox(text, caption, MessageBoxButton.OK, callback);
        }

        public void ShowMessageBox(string text, string caption, MessageBoxButton buttons, Action<MessageBoxResult> callback)
        {
            ShowMessageBox(text, caption, buttons, MessageBoxImage.None, callback);
        }

        public void ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon, Action<MessageBoxResult> callback)
        {
            ShowMessageBox(text, caption, buttons, icon, MessageBoxResult.None, callback);
        }

        public void ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult, Action<MessageBoxResult> callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            var result = System.Windows.MessageBox.Show(text, caption, buttons, icon, defaultResult);

            try
            {
                callback(result);
            }
            catch (Exception ex)
            {
                _log.Error("Error invoking callback.", ex);
            }
        }

        public MessageBoxResult ShowMessageBox(string text)
        {
            return ShowMessageBox(text, String.Empty);
        }

        public MessageBoxResult ShowMessageBox(string text, string caption)
        {
            return ShowMessageBox(text, caption, MessageBoxButton.OK);
        }

        public MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons)
        {
            return ShowMessageBox(text, caption, buttons, MessageBoxImage.None);
        }

        public MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
        {
            return ShowMessageBox(text, caption, buttons, icon, MessageBoxResult.None);
        }

        public MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return System.Windows.MessageBox.Show(text, caption, buttons, icon, defaultResult);
        }

        public void ShowError(string text)
        {
            System.Windows.MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowFolderBrowserDialog(string defaultPath, string description, bool showNewFolderButton, Action<DialogResult, string> callback)
        {
            if (callback == null) 
                throw new ArgumentNullException("callback");

            using (var dlg = new FolderBrowserDialog())
            {
                dlg.SelectedPath = defaultPath;
                dlg.Description = description;
                dlg.ShowNewFolderButton = showNewFolderButton;

                var result = dlg.ShowDialog();

                try
                {
                    callback(result, dlg.SelectedPath);
                }
                catch (Exception ex)
                {
                    _log.Error("Error invoking callback.", ex);
                }
            }
        }

        public void ShowOpenFileDialog(string defaultExt, string filter, Action<DialogResult, string> callback)
        {
            if (callback == null) 
                throw new ArgumentNullException("callback");

            using (var dlg = new OpenFileDialog())
            {
                dlg.DefaultExt = defaultExt;
                dlg.Filter = filter;

                var result = dlg.ShowDialog();

                try
                {
                    callback(result, dlg.FileName);
                }
                catch (Exception ex)
                {
                    _log.Error("Error invoking callback.", ex);
                }
            }
        }
    }
}

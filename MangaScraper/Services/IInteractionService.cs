using System;
using System.Windows;
using System.Windows.Forms;

namespace Blacker.MangaScraper.Services
{
    internal interface IInteractionService
    {
        void ShowMessageBox(string text, Action<MessageBoxResult> callback);
        void ShowMessageBox(string text, string caption, Action<MessageBoxResult> callback);
        void ShowMessageBox(string text, string caption, MessageBoxButton buttons, Action<MessageBoxResult> callback);
        void ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon, Action<MessageBoxResult> callback);
        void ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult, Action<MessageBoxResult> callback);

        MessageBoxResult ShowMessageBox(string text);
        MessageBoxResult ShowMessageBox(string text, string caption);
        MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons);
        MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon);
        MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult);
        
        void ShowError(string text);
        
        void ShowFolderBrowserDialog(string defaultPath, string description, bool showNewFolderButton, Action<DialogResult, string> callback);
        void ShowOpenFileDialog(string defaultExt, string filter, Action<DialogResult, string> callback);
    }
}
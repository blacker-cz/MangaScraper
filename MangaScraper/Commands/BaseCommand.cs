using System;
using System.Windows.Input;
using Blacker.MangaScraper.ViewModel;

namespace Blacker.MangaScraper.Commands
{
    /// <summary>
    /// Base class for commands implementation
    /// </summary>
    abstract class BaseCommand : ICommand
    {
        protected readonly BaseViewModel _viewModel;

        public bool Disabled { get; set; }

        public BaseCommand(BaseViewModel viewModel, bool disabled = false)
        {
            _viewModel = viewModel;
            Disabled = disabled;
        }

        public abstract void Execute(object parameter);

        public bool CanExecute(object parameter)
        {
            return !Disabled;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}

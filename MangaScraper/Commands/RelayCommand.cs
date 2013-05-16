using System;
using System.Windows.Input;

namespace Blacker.MangaScraper.Commands
{
    class RelayCommand : ICommand
    {
        private readonly Func<object, bool> _canExecuteDelegate;
        private readonly Action<object> _executeDelegate;

        public RelayCommand(Action<object> executeDelegate)
            : this(executeDelegate, false)
        {
        }

        public RelayCommand(Action<object> executeDelegate, bool disabled)
        {
            if (executeDelegate == null)
                throw new ArgumentNullException("executeDelegate");

            _executeDelegate = executeDelegate;
            _canExecuteDelegate = x => !Disabled;

            Disabled = disabled;
        }

        public RelayCommand(Action<object> executeDelegate, Func<object, bool> canExecuteDelegate)
        {
            if (executeDelegate == null) 
                throw new ArgumentNullException("executeDelegate");

            if (canExecuteDelegate == null) 
                throw new ArgumentNullException("canExecuteDelegate");

            _executeDelegate = executeDelegate;
            _canExecuteDelegate = canExecuteDelegate;
        }

        public bool Disabled { get; set; }

        public void Execute(object parameter)
        {
            _executeDelegate(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteDelegate(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}

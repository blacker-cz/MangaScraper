using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.MangaScraper.ViewModel;
using System.Windows;

namespace Blacker.MangaScraper.Commands
{
    class ClearCommand : BaseCommand
    {
        private bool _confirm;
        private string _confirmMessage;

        public ClearCommand(BaseViewModel model, bool disabled = false, bool confirm = false, string confirmMessage = "Do you really want to clear all items?")
            : base (model, disabled)
        {
            _confirm = confirm;
            _confirmMessage = confirmMessage;
        }

        public override void Execute(object parameter)
        {
            if (_confirm)
            {
                var result = MessageBox.Show(_confirmMessage, "Confirm action", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                    return;
            }
            ((IClearCommand)_viewModel).ClearClicked(parameter);
        }
    }
}

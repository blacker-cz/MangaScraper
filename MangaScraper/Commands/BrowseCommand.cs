using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.MangaScraper.ViewModel;

namespace Blacker.MangaScraper.Commands
{
    class BrowseCommand : BaseCommand
    {
        public BrowseCommand(BaseViewModel model, bool disabled = false)
            : base (model, disabled)
        { }

        public override void Execute(object parameter)
        {
            ((MainWindowViewModel)_viewModel).BrowseClicked();
        }
    }
}

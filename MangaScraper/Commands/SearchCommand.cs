using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.MangaScraper.Commands
{
    class SearchCommand : BaseCommand
    {
        public SearchCommand(BaseViewModel model, bool disabled = false)
            : base(model, disabled)
        { }

        public override void Execute(object parameter)
        {
            ((MainWindowViewModel)_viewModel).SearchManga();
        }
    }
}

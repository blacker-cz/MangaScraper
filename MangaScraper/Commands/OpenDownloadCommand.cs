using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.MangaScraper.ViewModel;

namespace Blacker.MangaScraper.Commands
{
    class OpenDownloadCommand : BaseCommand
    {
        public OpenDownloadCommand(BaseViewModel model, bool disabled = false)
            : base(model, disabled)
        { }

        public override void Execute(object parameter)
        {
            ((DownloadViewModel)_viewModel).Open();
        }
    }
}

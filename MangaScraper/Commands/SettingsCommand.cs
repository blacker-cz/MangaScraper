using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.MangaScraper.ViewModel;
using Blacker.MangaScraper.View;

namespace Blacker.MangaScraper.Commands
{
    class SettingsCommand : BaseCommand
    {
        public SettingsCommand(BaseViewModel model, bool disabled = false)
            : base(model, disabled)
        { }

        public override void Execute(object parameter)
        {
            var settingsWindow = new SettingsWindow();
        }
    }
}

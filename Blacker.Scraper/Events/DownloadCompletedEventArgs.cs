using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Events
{
    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool Cancelled { get; set; }
        public Exception Error { get; set; }
    }
}

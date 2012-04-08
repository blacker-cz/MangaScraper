using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Events
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public int PercentComplete { get; set; }
        public string Message { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Events
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public int Done { get; set; }
        public int From { get; set; }
        public string Action { get; set; }
    }
}

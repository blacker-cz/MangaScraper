using System;

namespace Blacker.MangaScraper.Common.Events
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public int PercentComplete { get; set; }
        public string Message { get; set; }
    }
}

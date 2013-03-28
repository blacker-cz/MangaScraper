using System;

namespace Blacker.MangaScraper.Common.Events
{
    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool Cancelled { get; set; }
        public Exception Error { get; set; }
    }
}

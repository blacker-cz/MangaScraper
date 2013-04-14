using System;
using System.Data;
using Blacker.MangaScraper.Common.Models;

namespace Blacker.MangaScraper.Library.Models
{
    internal class MangaRecord : IMangaRecord, IEquatable<IMangaRecord>
    {
        public string MangaId { get; set; }

        public string MangaName { get; set; }

        public string Url { get; set; }

        public Guid Scraper { get; set; }

        public bool Equals(IMangaRecord other)
        {
            if (other == null)
                return false;

            return other.MangaId == MangaId && other.Scraper == Scraper;
        }
    }
}

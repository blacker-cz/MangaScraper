using System;

namespace Blacker.MangaScraper.Common.Models
{
    public interface IMangaRecord
    {
        string MangaId { get; }

        string MangaName { get; }

        string Url { get; }

        Guid Scraper { get; }
    }
}
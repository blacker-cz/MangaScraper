using System;

namespace Blacker.MangaScraper.Common.Models
{
    public interface IChapterRecord
    {
        string MangaName { get; }

        string ChapterName { get; }

        string Url { get; }

        IMangaRecord MangaRecord { get; }

        Guid Scraper { get; }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Models
{
    public class ChapterRecord
    {
        public ChapterRecord(Scrapers scraper)
        {
            Scraper = scraper;
        }

        public string MangaName { get; set; }

        public string ChapterName { get; set; }

        public string Url { get; set; }

        public MangaRecord MangaRecord { get; set; }

        public Scrapers Scraper { get; private set; }
    }
}

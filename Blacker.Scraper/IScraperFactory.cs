﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper
{
    public interface IScraperFactory
    {
        IEnumerable<IScraper> GetScrapers();
    }
}
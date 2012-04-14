using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper
{
    public interface IPreload
    {
        /// <summary>
        /// Preload manga directory to cache, so the subsequent searches are quicker
        /// </summary>
        void PreloadDirectory();
    }
}

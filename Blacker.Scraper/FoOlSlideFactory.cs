using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper
{
    public class FoOlSlideFactory : IScraperFactory
    {
        // todo: make this configurable
        private readonly IEnumerable<FoOlSlide.FoOlSlideConfig> _configurations = new List<FoOlSlide.FoOlSlideConfig>()
                                                    {
                                                        new FoOlSlide.FoOlSlideConfig()
                                                            {
                                                                BaseUrl = "http://reader.vortex-scans.com/",
                                                                DirectoryUrl = "http://reader.vortex-scans.com/reader/list",
                                                                Name = "Vortex-Scans",
                                                                ScraperGuid = Guid.Parse("05345360-6ee0-4b74-8378-a69d38700ede")
                                                            },
                                                        new FoOlSlide.FoOlSlideConfig()
                                                            {
                                                                BaseUrl = "http://manga.redhawkscans.com",
                                                                DirectoryUrl = "http://manga.redhawkscans.com/reader/list",
                                                                Name = "Red Hawk Scans",
                                                                ScraperGuid = Guid.Parse("e4d2f38b-9e81-4b9a-beb1-bcd09b6388d9")
                                                            }
                                                    };

        private IEnumerable<IScraper> _scrapers;

        public IEnumerable<IScraper> GetScrapers()
        {
            return _scrapers ?? (_scrapers = _configurations.Select(conf => new FoOlSlide(conf)).ToList());
        }
    }
}

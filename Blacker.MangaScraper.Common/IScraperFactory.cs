using System.Collections.Generic;

namespace Blacker.MangaScraper.Common
{
    public interface IScraperFactory
    {
        IEnumerable<IScraper> GetScrapers();
    }
}

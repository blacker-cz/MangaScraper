namespace Blacker.MangaScraper.Common
{
    /// <summary>
    /// Use this interface to exclude scraper from automatic loading
    /// (this is useful e.g. when scraper needs to be initialized by some factory).
    /// </summary>
    public interface IScraperIgnore
    { }
}

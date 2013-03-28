namespace Blacker.MangaScraper.Common
{
    public interface IPreload
    {
        /// <summary>
        /// Preload manga directory to cache, so the subsequent searches are quicker
        /// </summary>
        void PreloadDirectory();
    }
}

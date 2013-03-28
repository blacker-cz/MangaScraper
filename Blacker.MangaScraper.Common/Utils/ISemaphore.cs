namespace Blacker.MangaScraper.Common.Utils
{
    public interface ISemaphore
    {
        bool Wait();

        bool Wait(int milliseconds);

        void Release();

        void Release(int count);
    }
}

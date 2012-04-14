using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Utils
{
    public interface ISemaphore
    {
        bool Wait();

        bool Wait(int milliseconds);

        void Release();

        void Release(int count);
    }
}

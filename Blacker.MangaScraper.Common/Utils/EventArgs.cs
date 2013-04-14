using System;

namespace Blacker.MangaScraper.Common.Utils
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T value)
        {
            _value = value;
        }

        private readonly T _value;

        public T Value
        {
            get { return _value; }
        }
    }
}

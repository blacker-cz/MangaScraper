using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Timers;

namespace Blacker.MangaScraper.Common.Utils
{
    /// <summary>
    /// Simple cache implementation
    /// </summary>
    public class Cache<TKey, TValue> : IDisposable
    {
        public static readonly TimeSpan DefaultTimeout = new TimeSpan(0, 5, 0);

        private readonly Timer _timer;
        private readonly ConcurrentDictionary<TKey, CachedObject<TValue>> _dict;

        #region Constructors

        public Cache()
            : this(DefaultTimeout)
        { }

        public Cache(TimeSpan timeout)
        {
            _dict = new ConcurrentDictionary<TKey, CachedObject<TValue>>();

            Timeout = timeout;
            _timer = new Timer(500) { AutoReset = true };
            _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
            _timer.Enabled = true;
        }

        #endregion // Constructors

        /// <summary>
        /// Clean invalid records from cache
        /// </summary>
        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var invalidRecords = _dict.Where(kvp => !kvp.Value.IsValid).Select(kvp => kvp.Key).ToList();

            foreach (var key in invalidRecords)
            {
                Remove(key);
            }
        }

        public TimeSpan Timeout { get; set; }

        public Action<TValue> RemovalCallback { get; set; }

        public TValue this[TKey key]
        {
            get
            {
                CachedObject<TValue> value;
                if (_dict.TryGetValue(key, out value))
                {
                    if (value.IsValid)
                    {
                        return value.Value;
                    }
                    else
                    {
                        Remove(key);
                        return default(TValue);
                    }

                }
                else
                {
                    return default(TValue);
                }
            }
            set
            {
                Remove(key);

                if (value == null)
                    return;

                _dict.TryAdd(key, new CachedObject<TValue>(Timeout, value));
            }
        }

        public void Add(TKey key, TValue value)
        {
            Add(key, value, Timeout);
        }

        public void Add(TKey key, TValue value, TimeSpan timeout)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (!_dict.TryAdd(key, new CachedObject<TValue>(timeout, value)))
                throw new ArgumentException("An element with the same key already exists in the Cache.", "key");
        }

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public void Remove(TKey key)
        {
            CachedObject<TValue> value;

            if(!_dict.TryRemove(key, out value))
                return;

            var removalCallback = RemovalCallback;
            if (removalCallback != null)
            {
                try
                {
                    removalCallback(value.Value);
                }
                catch
                {
                    // make sure that broken callback doesn't break anything else
                }
            }
        }

        #region IDisposable implementation

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if(disposing)
                {
                    // Dispose managed resources.
                    if (_timer != null)
                    {
                        _timer.Enabled = false;
                        _timer.Dispose();
                    }
                }
                // Note disposing has been done.
                disposed = true;

            }
        }

        ~Cache()
        {
            Dispose(false);
        }

        #endregion // IDisposable implementation

        private class CachedObject<TVal>
        {
            public CachedObject(TimeSpan timeout, TVal value)
            {
                Created = DateTime.UtcNow;
                Timeout = timeout;
                Value = value;
            }

            public DateTime Created { get; set; }
            public TimeSpan Timeout { get; set; }
            public TVal Value { get; set; }

            public bool IsValid
            {
                get { return Created + Timeout > DateTime.UtcNow; }
            }
        }
    }
}

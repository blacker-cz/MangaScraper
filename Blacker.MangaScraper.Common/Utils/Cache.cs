using System;
using System.Collections.Generic;
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

        private readonly object _syncRoot = new Object();
        private readonly Timer _timer;

        private readonly Dictionary<TKey, CachedObject<TValue>> _dict;

        #region Constructors

        public Cache()
            : this(DefaultTimeout)
        { }

        public Cache(TimeSpan timeout)
        {
            _dict = new Dictionary<TKey, CachedObject<TValue>>();

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
            lock (_syncRoot)
            {
                var invalidRecords = _dict.Where(kvp => !kvp.Value.IsValid).Select(kvp => kvp.Key).ToList();

                foreach (var key in invalidRecords)
                {
                    Remove(key);
                }
            }
        }

        public TimeSpan Timeout { get; set; }

        public TValue this[TKey key]
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_dict.ContainsKey(key))
                    {
                        var value = _dict[key];
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
            }
            set
            {
                lock (_syncRoot)
                {
                    Remove(key);

                    if (value == null)
                        return;

                    _dict.Add(key, new CachedObject<TValue>(Timeout, value));
                }
            }
        }

        public void Remove(TKey key)
        {
            lock (_syncRoot)
            {
                if(!_dict.ContainsKey(key))
                    return;

                var value = _dict[key];

                // dispose if cached object is disposable
                if (value.Value is IDisposable)
                    (value.Value as IDisposable).Dispose();

                _dict.Remove(key);
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

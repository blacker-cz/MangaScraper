using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Blacker.Scraper.Utils
{
    /// <summary>
    /// Implementation of FIFO Semaphore.
    /// Based on implementation from DigitallyCreated Utilities (http://dcutilities.codeplex.com/)
    /// </summary>
    public class FifoSemaphore : ISemaphore
    {
        private readonly Queue<Waiter> _waitQueue;

        private readonly object _syncRoot = new object();

        /// <summary>
        /// The tokens count for this class
        /// </summary>
        protected int _tokens;

        /// <summary>
        /// Constructor, creates a FifoSemaphore
        /// </summary>
        /// <param name="tokens">The number of tokens the semaphore will start with</param>
        public FifoSemaphore(int tokens)
        {
            _tokens = tokens;
            _waitQueue = new Queue<Waiter>();
        }

        public bool Wait()
        {
            return Wait(Timeout.Infinite);
        }

        public bool Wait(int milliseconds)
        {
            Waiter waiter;

            lock (_syncRoot)
            {
                if (_tokens > 0)
                {
                    --_tokens;
                    return true;
                }

                waiter = new Waiter();
                _waitQueue.Enqueue(waiter);
            }

            return waiter.TryWait(milliseconds);
        }

        public void Release()
        {
            Release(1);
        }

        public void Release(int count)
        {
            lock (_syncRoot)
            {
                for (int i = 0; i < count; i++)
                {
                    if (_waitQueue.Count > 0)
                    {
                        Waiter waiter = _waitQueue.Dequeue();
                        bool releasedSuccessfully = waiter.Release();

                        if (!releasedSuccessfully) //That thread was interrupted or timed out!
                            i--; //Try again with the next
                    }
                    else
                    {
                        //We've got no one waiting, so add a token
                        _tokens++;
                    }
                }
            }
        }

        /// <summary>
        /// Waiter helper class that allows threads to queue for tokens
        /// </summary>
        private class Waiter
        {
            private readonly object _syncRoot = new object();
            private bool _released;

            public Waiter()
            {
                _released = false;
            }

            public bool TryWait(int millisecondsTimeout)
            {
                lock (_syncRoot)
                {
                    if (_released) //We've been released before we even started waiting!
                        return true;

                    try
                    {
                        bool success = Monitor.Wait(_syncRoot, millisecondsTimeout);

                        if (!success)
                            _released = true; //Note that we've been released early

                        return success;
                    }
                    catch (ThreadInterruptedException)
                    {
                        if (_released == false)
                        {
                            _released = true; //Note that we've been released early
                            throw;
                        }

                        //We've already been released, so we might as well succeed at
                        //the operation and get interrupted later
                        Thread.CurrentThread.Interrupt();
                        return true;
                    }
                }
            }

            public bool Release()
            {
                lock (_syncRoot)
                {
                    if (_released) //If released already (this means we've been interrupted or we timed out!)
                        return false;

                    _released = true;
                    Monitor.Pulse(_syncRoot);
                    return true;
                }
            }
        }
    }
}

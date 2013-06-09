using System;
using System.Threading;
using log4net;

namespace Blacker.Scraper.Helpers
{
    internal static class RetryHelper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (RetryHelper));

        public const int DefaultRetries = 3;
        public const int DefaultRetryTimeoutMs = 500;

        public static void Retry(Action action)
        {
            Retry(action, DefaultRetries, DefaultRetryTimeoutMs);
        }

        public static void Retry(Action action, int numRetries)
        {
            Retry(action, numRetries, DefaultRetryTimeoutMs);
        }

        public static void Retry(Action action, int numRetries, int retryTimeout)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            do
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    _log.Warn("Action failed in retry helper.", ex);

                    if (--numRetries <= 0)
                        throw;

                    Thread.Sleep(retryTimeout);
                }
            } while (true);
        }

        public static T Retry<T>(Func<T> func)
        {
            return Retry<T>(func, DefaultRetries);
        }

        public static T Retry<T>(Func<T> func, int numRetries)
        {
            return Retry<T>(func, numRetries, DefaultRetryTimeoutMs);
        }

        public static T Retry<T>(Func<T> func, int numRetries, int retryTimeout)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            do
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    _log.Warn("Function failed in retry helper.", ex);

                    if (--numRetries <= 0)
                        throw;

                    Thread.Sleep(retryTimeout);
                }
            } while (true);
        }
    }
}

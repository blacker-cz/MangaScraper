using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using log4net;

namespace Blacker.MangaScraper.Helpers
{
    static class AsyncWrapper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AsyncWrapper));

        public static void Call<TResult>(Func<TResult> method, Action<TResult, Exception> callback)
        {
            if (method == null)
                throw new ArgumentNullException("method");
            if (callback == null)
                throw new ArgumentNullException("callback");

            method.BeginInvoke(new AsyncCallback(FuncCallAsyncCallback<TResult>), callback);
        }

        private static void FuncCallAsyncCallback<TResult>(IAsyncResult result)
        {
            var callback = result.AsyncState as Action<TResult, Exception>;
            var deleg = (result as AsyncResult).AsyncDelegate as Func<TResult>;
            if (deleg != null && callback != null)
            {
                try
                {
                    TResult retval = deleg.EndInvoke(result);

                    callback(retval, null);
                }
                catch (Exception ex)
                {
                    _log.Error("There was en error during the asynchronous operation.", ex);
                    callback(default(TResult), ex);
                }
            }
            else if (callback != null)
            {
                _log.Error("Invalid delegate.");
                callback(default(TResult), new InvalidOperationException());
            }
            else
            {
                _log.Error("Invalid callback method");
            }
        }
    }
}

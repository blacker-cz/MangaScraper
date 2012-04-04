using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;

namespace Blacker.MangaScraper.Helpers
{
    class AsyncWrapper
    {
        public void Call<TResult>(Func<TResult> method, Action<TResult, Exception> callback)
        {
            if (method == null)
                throw new ArgumentNullException("method");
            if (callback == null)
                throw new ArgumentNullException("callback");

            method.BeginInvoke(new AsyncCallback(FuncCallAsyncCallback<TResult>), callback);
        }

        private void FuncCallAsyncCallback<TResult>(IAsyncResult result)
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
                    callback(default(TResult), ex);
                }
            }
            else if (callback != null)
            {
                callback(default(TResult), new InvalidOperationException());
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Exceptions
{
    [Serializable]
    public class HttpException : Exception
    {
        public HttpException() { }

        public HttpException(string message) : base(message) { }

        public HttpException(string message, Exception inner) : base(message, inner) { }

        protected HttpException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}

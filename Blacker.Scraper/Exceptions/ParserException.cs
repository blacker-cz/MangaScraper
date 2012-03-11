using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.Scraper.Exceptions
{
    [Serializable]
    public class ParserException : Exception
    {
        public string HtmlDump { get; private set; }

        public ParserException() { }

        public ParserException(string message) : base(message) { }

        public ParserException(string message, string htmlDump)
            : base(message)
        {
            HtmlDump = htmlDump;
        }

        public ParserException(string message, Exception inner) : base(message, inner) { }

        protected ParserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}

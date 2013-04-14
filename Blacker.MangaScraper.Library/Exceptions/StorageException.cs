using System;
using System.Runtime.Serialization;

namespace Blacker.MangaScraper.Library.Exceptions
{
    [Serializable]
    public class StorageException : Exception
    {
        public StorageException()
        {
        }

        public StorageException(string message)
            : base(message)
        {
        }

        public StorageException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected StorageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

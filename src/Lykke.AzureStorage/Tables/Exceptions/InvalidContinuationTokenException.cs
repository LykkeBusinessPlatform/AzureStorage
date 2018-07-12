using System;
using System.Runtime.Serialization;

namespace Lykke.AzureStorage.Tables.Exceptions
{
    /// <summary>
    /// Exception for a case when invalid continuation token is provided.
    /// </summary>
    public class InvalidContinuationTokenException : Exception
    {
        public InvalidContinuationTokenException()
        {
        }

        public InvalidContinuationTokenException(string message) : base(message)
        {
        }

        public InvalidContinuationTokenException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidContinuationTokenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

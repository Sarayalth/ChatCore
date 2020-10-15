using System;
using System.Runtime.Serialization;

namespace ChatCore.Exceptions
{
    [Serializable]
    public class ChatCoreNotInitializedException : Exception
    {
        public ChatCoreNotInitializedException()
        {
        }

        public ChatCoreNotInitializedException(string message) : base(message)
        {
        }

        public ChatCoreNotInitializedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ChatCoreNotInitializedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

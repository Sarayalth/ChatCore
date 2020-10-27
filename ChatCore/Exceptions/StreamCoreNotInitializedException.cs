﻿using System;
using System.Runtime.Serialization;

namespace ChatCore.Exceptions
{
    [Serializable]
    public class StreamCoreNotInitializedException : Exception
    {
        public StreamCoreNotInitializedException()
        {
        }

        public StreamCoreNotInitializedException(string message) : base(message)
        {
        }

        public StreamCoreNotInitializedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected StreamCoreNotInitializedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

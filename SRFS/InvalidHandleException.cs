using System;

namespace SRFS {

    public class InvalidHandleException : Exception {
        public InvalidHandleException() : base() { }
        public InvalidHandleException(string message, Exception innerException) : base(message, innerException) { }
    }
}

using System;
using System.Runtime.Serialization;

namespace Gehtsoft.Tools2.Algorithm.DFA
{
    /// <summary>
    /// Digital Final Automate exception.
    /// </summary>
    [Serializable]
    public class DFAException : Exception
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public DFAException() : base() { }
        /// <summary>
        /// Constructor with a message
        /// </summary>

        public DFAException(string message) : base(message) { }

        /// <summary>
        /// Constructor with an internal exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ie"></param>
        public DFAException(string message, Exception ie) : base(message, ie) { }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DFAException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

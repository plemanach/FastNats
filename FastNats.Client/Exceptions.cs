using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastNats.Client
{
    /// <summary>
    /// The exception that is thrown when there is a NATS error condition.  All
    /// NATS exception inherit from this class.
    /// </summary>
    public class FastNATSException : Exception
    {
        internal FastNATSException() : base() { }
        internal FastNATSException(string err) : base(err) { }
        internal FastNATSException(string err, Exception innerEx) : base(err, innerEx) { }
    }

    /// <summary>
    /// The exception that is thrown when there is a connection error.
    /// </summary>
    public class FastNATSConnectionException : FastNATSException
    {
        internal FastNATSConnectionException(string err) : base(err) { }
        internal FastNATSConnectionException(string err, Exception innerEx) : base(err, innerEx) { }
    }

    /// <summary>
    /// This exception that is thrown when there is an internal error with
    /// the NATS protocol.
    /// </summary>
    public class FastNATSProtocolException : FastNATSException
    {
        internal FastNATSProtocolException(string err) : base(err) { }
    }

    /// <summary>
    /// The exception that is thrown when a connection cannot be made
    /// to any server.
    /// </summary>
    public class FastNATSNoServersException : FastNATSException
    {
        internal FastNATSNoServersException(string err) : base(err) { }
    }

    /// <summary>
    /// The exception that is thrown when a secure connection is requested,
    /// but not required.
    /// </summary>
    public class FastNATSSecureConnWantedException : FastNATSException
    {
        internal FastNATSSecureConnWantedException() : base("A secure connection is requested.") { }
    }

    /// <summary>
    /// The exception that is thrown when a secure connection is required.
    /// </summary>
    public class FastNATSSecureConnRequiredException : FastNATSException
    {
        internal FastNATSSecureConnRequiredException() : base("A secure connection is required.") { }
        internal FastNATSSecureConnRequiredException(String s) : base(s) { }
    }

    /// <summary>
    /// The exception that is thrown when a an operation is performed on
    /// a connection that is closed.
    /// </summary>
    public class FastNATSConnectionClosedException : FastNATSException
    {
        internal FastNATSConnectionClosedException() : base("Connection is closed.") { }
    }

    /// <summary>
    /// The exception that is thrown when a consumer (subscription) is slow.
    /// </summary>
    public class FastNATSSlowConsumerException : FastNATSException
    {
        internal FastNATSSlowConsumerException() : base("Consumer is too slow.") { }
    }

    /// <summary>
    /// The exception that is thrown when an operation occurs on a connection
    /// that has been determined to be stale.
    /// </summary>
    public class FastNATSStaleConnectionException : FastNATSException
    {
        internal FastNATSStaleConnectionException() : base("Connection is stale.") { }
    }

    /// <summary>
    /// The exception that is thrown when a message payload exceeds what
    /// the maximum configured.
    /// </summary>
    public class FastNATSMaxPayloadException : FastNATSException
    {
        internal FastNATSMaxPayloadException() : base("Maximum payload size has been exceeded") { }
        internal FastNATSMaxPayloadException(string err) : base(err) { }
    }

    /// <summary>
    /// The exception that is thrown when a subscriber has exceeded the maximum
    /// number of messages that has been configured.
    /// </summary>
    public class FastNATSMaxMessagesException : FastNATSException
    {
        internal FastNATSMaxMessagesException() : base("Maximum number of messages have been exceeded.") { }
    }

    /// <summary>
    /// The exception that is thrown when a subscriber operation is performed on
    /// an invalid subscriber.
    /// </summary>
    public class FastNATSBadSubscriptionException : FastNATSException
    {
        internal FastNATSBadSubscriptionException() : base("Subcription is not valid.") { }
    }

    /// <summary>
    /// The exception that is thrown when a NATS operation times out.
    /// </summary>
    public class FastNATSTimeoutException : FastNATSException
    {
        internal FastNATSTimeoutException() : base("Timeout occurred.") { }
    }
}

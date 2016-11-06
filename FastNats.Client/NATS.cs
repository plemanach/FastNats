﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastNats.Client
{
    /// <summary>
    /// This class contains default values for fields used throughout NATS.
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Client version
        /// </summary>
        public const string Version = "0.0.1";

        /// <summary>
        /// The default NATS connect url ("nats://localhost:4222")
        /// </summary>
        public const string Url = "nats://localhost:4222";

        /// <summary>
        /// The default NATS connect port. (4222)
        /// </summary>
        public const int Port = 4222;

        /// <summary>
        /// Default number of times to attempt a reconnect. (60)
        /// </summary>
        public const int MaxReconnect = 60;

        /// <summary>
        /// Default ReconnectWait time (2 seconds)
        /// </summary>
        public const int ReconnectWait = 2000; // 2 seconds.

        /// <summary>
        /// Default timeout  (2 seconds).
        /// </summary>
        public const int Timeout = 2000; // 2 seconds.

        /// <summary>
        ///  Default ping interval (2 minutes);
        /// </summary>
        public const int PingInterval = 120000;// 2 minutes.

        /// <summary>
        /// Default MaxPingOut value (2);
        /// </summary>
        public const int MaxPingOut = 2;

        /// <summary>
        /// Default MaxChanLen (65536)
        /// </summary>
        public const int MaxChanLen = 65536;

        /// <summary>
        /// Default Request Channel Length
        /// </summary>
        public const int RequestChanLen = 4;

        /// <summary>
        /// Language string of this client, ".NET"
        /// </summary>
        public const string LangString = ".NET";

        /// <summary>
        /// Default subscriber pending messages limit.
        /// </summary>
        public const long SubPendingMsgsLimit = 65536;

        /// <summary>
        /// Default subscriber pending bytes limit.
        /// </summary>
        public const long SubPendingBytesLimit = 65536 * 1024;

        /*
         * Namespace level defaults
         */

        // Scratch storage for assembling protocol headers
        internal const int scratchSize = 512;

        // The size of the bufio writer on top of the socket.
        internal const int defaultBufSize = 32768;

        // The read size from the network stream.
        internal const int defaultReadLength = 20480;

        // The size of the bufio while we are reconnecting
        internal const int defaultPendingSize = 1024 * 1024;

        // Default server pool size
        internal const int srvPoolSize = 4;
    }

    /// <summary>
    /// Event arguments for the ConnEventHandler type delegate.
    /// </summary>
    public class ConnEventArgs : EventArgs
    {
        private Connection c;

        internal ConnEventArgs(Connection c)
        {
            this.c = c;
        }

        /// <summary>
        /// Gets the connection associated with the event.
        /// </summary>
        public Connection Conn
        {
            get { return c; }
        }
    }

    /// <summary>
    /// Event arguments for the ErrorEventHandler type delegate.
    /// </summary>
    public class ErrEventArgs : EventArgs
    {
        private Connection c;
        private Subscription s;
        private String err;

        internal ErrEventArgs(Connection c, Subscription s, String err)
        {
            this.c = c;
            this.s = s;
            this.err = err;
        }

        /// <summary>
        /// Gets the connection associated with the event.
        /// </summary>
        public Connection Conn
        {
            get { return c; }
        }

        /// <summary>
        /// Gets the Subscription associated wit the event.
        /// </summary>
        public Subscription Subscription
        {
            get { return s; }
        }

        /// <summary>
        /// Gets the error associated with the event.
        /// </summary>
        public string Error
        {
            get { return err; }
        }
    }

    /**
     * Internal Constants
     */
    internal class IC
    {
        internal const string _CRLF_ = "\r\n";
        internal const string _EMPTY_ = "";
        internal const string _SPC_ = " ";
        internal const string _PUB_P_ = "PUB ";

        internal const string _OK_OP_ = "+OK";
        internal const string _ERR_OP_ = "-ERR";
        internal const string _MSG_OP_ = "MSG";
        internal const string _PING_OP_ = "PING";
        internal const string _PONG_OP_ = "PONG";
        internal const string _INFO_OP_ = "INFO";

        internal const string inboxPrefix = "_INBOX.";

        internal const string conProto = "CONNECT {0}" + IC._CRLF_;
        internal const string pingProto = "PING" + IC._CRLF_;
        internal const string pongProto = "PONG" + IC._CRLF_;
        internal const string pubProto = "PUB {0} {1} {2}" + IC._CRLF_;
        internal const string subProto = "SUB {0} {1} {2}" + IC._CRLF_;
        internal const string unsubProto = "UNSUB {0} {1}" + IC._CRLF_;

        internal const string pongProtoNoCRLF = "PONG";
        internal const string okProtoNoCRLF = "+OK";

        internal const string STALE_CONNECTION = "stale connection";
        internal const string AUTH_TIMEOUT = "authorization timeout";
    }

    /// <summary>
    /// This class is passed into the MsgHandler delegate, providing the
    /// message received.
    /// </summary>
    public class MsgHandlerEventArgs : EventArgs
    {
        internal Msg msg = null;

        /// <summary>
        /// Retrieves the message.
        /// </summary>
        public Msg Message
        {
            get { return msg; }
        }
    }
}

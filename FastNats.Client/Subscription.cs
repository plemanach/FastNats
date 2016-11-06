﻿using System;
using System.Text;

namespace FastNats.Client
{
    public class Subscription : ISubscription, IDisposable
    {
        readonly internal Object mu = new Object(); // lock

        internal long sid = 0; // subscriber ID.
        private long msgs;
        internal protected long delivered;
        private long bytes;
        internal protected long max = -1;

        // slow consumer
        internal bool sc = false;

        internal Connection conn = null;
        internal bool closed = false;
        internal bool connClosed = false;

        internal Channel<Msg> mch = null;
        internal bool ownsChannel = true;

        // Pending stats, async subscriptions, high-speed etc.
        internal long pMsgs = 0;
        internal long pBytes = 0;
        internal long pMsgsMax = 0;
        internal long pBytesMax = 0;
        internal long pMsgsLimit = Defaults.SubPendingMsgsLimit;
        internal long pBytesLimit = Defaults.SubPendingBytesLimit;
        internal long dropped = 0;

        // Subject that represents this subscription. This can be different
        // than the received subject inside a Msg if this is a wildcard.
        private string subject = null;

        internal Subscription(Connection conn, string subject, string queue)
        {
            this.conn = conn;
            this.subject = subject;
            this.queue = queue;
        }

        internal virtual void Close()
        {
            Close(true);
        }

        internal void Close(bool closeChannel)
        {
            lock (mu)
            {
                if (closeChannel && mch != null)
                {
                    mch.Close();
                    mch = null;
                }
                closed = true;
                connClosed = true;
            }
        }

        public string Subject
        {
            get { return subject; }
        }

        // Optional queue group name. If present, all subscriptions with the
        // same name will form a distributed queue, and each message will
        // only be processed by one member of the group.
        string queue;

        public string Queue
        {
            get { return queue; }
        }

        public Connection Connection
        {
            get
            {
                return conn;
            }
        }

        internal bool tallyMessage(long bytes)
        {
            lock (mu)
            {
                if (max > 0 && msgs > max)
                    return true;

                this.msgs++;
                this.bytes += bytes;

            }

            return false;
        }


        protected internal virtual bool processMsg(Msg msg)
        {
            return true;
        }

        protected long tallyDeliveredMessage(Msg msg)
        {
            lock (mu)
            {
                delivered++;
                pBytes -= msg.Data.Length;
                pMsgs--;

                return delivered;
            }
        }

        // returns false if the message could not be added because
        // the channel is full, true if the message was added
        // to the channel.
        internal bool addMessage(Msg msg, int maxCount)
        {
            // Subscription internal stats
            pMsgs++;
            if (pMsgs > pMsgsMax)
            {
                pMsgsMax = pMsgs;
            }

            pBytes += msg.Data.Length;
            if (pBytes > pBytesMax)
            {
                pBytesMax = pBytes;
            }

            // Check for a Slow Consumer
            if (pMsgs > pMsgsLimit || pBytes > pBytesLimit)
            {
                 return false;
            }

            if (mch != null)
            {
                if (mch.Count >= maxCount)
                {
                    return false;
                }
                else
                {
                    sc = false;
                    mch.Add(msg);
                }
            }
            return true;
        }

        public bool IsValid
        {
            get
            {
                lock (mu)
                {
                    return (conn != null);
                }
            }
        }

        internal void unsubscribe(bool throwEx)
        {
            Connection c;
            lock (mu)
            {
                c = this.conn;
            }

            if (c == null)
            {
                if (throwEx)
                    throw new FastNATSBadSubscriptionException();

                return;
            }

            c.Unsubscribe(this, 0);
        }

        public virtual void Unsubscribe()
        {
            unsubscribe(true);
        }

        public virtual void AutoUnsubscribe(int max)
        {
            Connection c = null;

            lock (mu)
            {
                if (conn == null)
                    throw new FastNATSBadSubscriptionException();

                c = conn;
            }

            c.Unsubscribe(this, max);
        }

        public int QueuedMessageCount
        {
            get
            {
                lock (mu)
                {
                    if (this.conn == null)
                        throw new FastNATSBadSubscriptionException();

                    return mch.Count;
                }
            }
        }

        void IDisposable.Dispose()
        {
            try
            {
                Unsubscribe();
            }
            catch (Exception)
            {
                // We we get here with normal usage, for example when
                // auto unsubscribing, so just this here.
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{");

            sb.AppendFormat("Subject={0};Queue={1};" +
                "QueuedMessageCount={2};IsValid={3};Type={4}",
                Subject, (Queue == null ? "null" : Queue),
                QueuedMessageCount, IsValid,
                this.GetType().ToString());

            sb.Append("}");

            return sb.ToString();
        }

        private void checkState()
        {
            if (conn == null)
                throw new FastNATSBadSubscriptionException();
        }

        public void SetPendingLimits(long messageLimit, long bytesLimit)
        {
            lock (mu)
            {
                checkState();

                pMsgsLimit = messageLimit;
                pBytesLimit = bytesLimit;
            }
        }

        public long PendingByteLimit
        {
            get
            {
                lock (mu)
                {
                    checkState();
                    return pBytesLimit;
                }
            }
            set
            {
                lock (mu)
                {
                    checkState();
                    pBytesLimit = value;
                }
            }
        }

        public long PendingMessageLimit
        {
            get
            {
                lock (mu)
                {
                    checkState();
                    return pMsgsLimit;
                }
            }
            set
            {
                lock (mu)
                {
                    checkState();
                    pMsgsLimit = value;
                }
            }
        }

        public void GetPending(out long pendingBytes, out long pendingMessages)
        {
            lock (mu)
            {
                checkState();
                pendingBytes = pBytes;
                pendingMessages = pMsgs;
            }
        }

        public long PendingBytes
        {
            get
            {
                lock (mu)
                {
                    checkState();
                    return pBytes;
                }
            }
        }

        public long PendingMessages
        {
            get
            {
                lock (mu)
                {
                    checkState();
                    return pMsgs;
                }
            }
        }

        public void GetMaxPending(out long maxPendingBytes, out long maxPendingMessages)
        {
            lock (mu)
            {
                checkState();
                maxPendingBytes = pBytesMax;
                maxPendingMessages = pMsgsMax;
            }
        }

        public long MaxPendingBytes
        {
            get
            {
                lock (mu)
                {
                    checkState();
                    return pBytesMax;
                }
            }
        }


        public long MaxPendingMessages
        {
            get
            {
                lock (mu)
                {
                    checkState();
                    return pMsgsMax;
                }
            }
        }

        public void ClearMaxPending()
        {
            lock (mu)
            {
                pMsgsMax = pBytesMax = 0;
            }
        }

        public long Delivered
        {
            get
            {
                lock (mu)
                {
                    return delivered;
                }
            }
        }


        public long Dropped
        {
            get
            {
                lock (mu)
                {
                    return dropped;
                }
            }
        }

    }  // Subscription
}

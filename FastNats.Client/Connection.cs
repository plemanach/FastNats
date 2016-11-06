using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace FastNats.Client
{
    public class Connection : IConnection
    {
        Options _options = new Options();
        // NOTE: We aren't using Mutex here to support enterprises using
        // .NET 4.0.
        readonly private object _mu = new Object();
        volatile private ConnState _status = ConnState.CLOSED;
        private ConcurrentDictionary<Int64, Subscription> _subs =
         new ConcurrentDictionary<Int64, Subscription>();

        // we have a buffered reader for writing, and reading.
        // This is for both performance, and having to work around
        // interlinked read/writes (supported by the underlying network
        // stream, but not the BufferedStream).
        private Stream _bw = null;
        private Stream _br = null;
        private MemoryStream _pending = null;
        TCPConnection conn = new TCPConnection();
        Thread _readerThead;
        Exception _lastExcept;
        private Uri _url = null;
        ServerInfo _serverInfo;

        private bool createConn(Srv s)
        {
            try
            {
                _url = s.url;

                conn.open(s, _options.Timeout);

                if (_pending != null && _bw != null)
                {
                    // flush to the pending buffer;
                    try
                    {
                        // Make a best effort, but this shouldn't stop
                        // conn creation.
                        _bw.Flush();
                    }
                    catch (Exception) { }
                }

                _bw = conn.getWriteBufferedStream(Defaults.defaultBufSize);
                _br = conn.getReadBufferedStream();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        internal Connection(Options opts)
        {
            _options = opts;
        }


        internal void Connect()
        {
            if (createConn(new Srv(Defaults.Url)))
            {
                ProcessConnectInit();
            }
        }

        private void ReaderLoop()
        {
            byte[] buffer = new byte[Defaults.defaultReadLength];
            Parser parser = new Parser();

            parser.MsgReceived += (object sender, Msg e) => {


            };

            int len;

            while (true)
            {
                try
                {
                    len = _br.Read(buffer, 0, Defaults.defaultReadLength);
                    parser.parse(buffer, len);
                }
                catch (Exception e)
                {
                    if (State != ConnState.CLOSED)
                    {
                        ProcessOpError(e);
                    }
                    break;
                }
            }
        }

        private void ProcessConnectInit()
        {
            this._status = ConnState.CONNECTING;

            _serverInfo = ProcessExpectedInfo();
            SendConnect();

            // .NET vs go design difference here:
            // Starting the ping timer earlier allows us
            // to assign, and thus, dispose of it if the connection 
            // is disposed before the socket watchers are running.
            // Otherwise, the connection is referenced by an orphaned 
            // ping timer which can create a memory leak.
            //TODO:Implement timer
            //StartPingTimer();

            LaunchReaderLoop();
        }

        private void LaunchReaderLoop()
        {
            AutoResetEvent readerThreadStarted = new AutoResetEvent(false);

            _readerThead = new Thread(() =>
            {
                readerThreadStarted.Set();
                ReaderLoop();
            });

            _readerThead.Start();

            readerThreadStarted.WaitOne();
        }

        private void SendConnect()
        {
            WriteString(connectProto());
            WriteString(IC.pingProto);
            _bw.Flush();

            StreamReader sr = new StreamReader(_br);
            string result = sr.ReadLine();

            // If opts.verbose is set, handle +OK.
            if (_options.Verbose == true && IC.okProtoNoCRLF.Equals(result))
            {
                result = sr.ReadLine();
            }

            if (IC.pongProtoNoCRLF.Equals(result))
            {
                _status = ConnState.CONNECTED;
                return;
            }
            else
            {
                if (result == null)
                {
                    throw new FastNATSConnectionException("Connect read protocol error");
                }
                else if (result.StartsWith(IC._ERR_OP_))
                {
                    throw new FastNATSConnectionException(
                        result.Substring(IC._ERR_OP_.Length));
                }
                else
                {
                    throw new FastNATSException("Error from sendConnect(): " + result);
                }
            }
        }

        private ServerInfo ProcessExpectedInfo()
        {
            char[] separator = { ' ' };
            StreamReader sr = new StreamReader(_br);

            var serverInfo = sr.ReadLine();
            var parts = serverInfo.Split(separator, 2);

            if (parts.Length < 2)
            {
                return new ServerInfo();
            }
            else
            {
                return ServerInfo.CreateFromJson(parts[1]);
            }
        }



        private string connectProto()
        {
            string u = _url.UserInfo;
            string user = null;
            string pass = null;
            string token = null;

            if (!string.IsNullOrEmpty(u))
            {
                if (u.Contains(":"))
                {
                    string[] userpass = u.Split(':');
                    if (userpass.Length > 0)
                    {
                        user = userpass[0];
                    }
                    if (userpass.Length > 1)
                    {
                        pass = userpass[1];
                    }
                }
                else
                {
                    token = u;
                }
            }
            else
            {
                user =  _options.user;
                pass = _options.password;
                token = _options.token;
            }

            ConnectInfo info = new ConnectInfo(_options.Verbose, _options.Pedantic, user,
                pass, token, _options.Secure, _options.Name);

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat(IC.conProto, info.ToJson());
            return sb.ToString();
        }

        private void ProcessOpError(Exception e)
        {
            bool disconnected = false;

            lock (_mu)
            {
                if (IsConnecting() || IsClosed() || IsReconnecting())
                {
                    return;
                }

                if (Opts.AllowReconnect && _status == ConnState.CONNECTED)
                {
                    ProcessReconnect();
                }
                else
                {
                    ProcessDisconnect();
                    disconnected = true;
                    _lastExcept = e;
                }
            }

            if (disconnected)
            {
                Close();
            }
        }


        private void ProcessDisconnect()
        {

        }
        private void ProcessReconnect()
        {
        }

        public string ConnectedId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ConnectedUrl
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string[] DiscoveredServers
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Exception LastError
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public long MaxPayload
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Options Opts
        {
            get
            {
                return _options;
            }
        }

        public string[] Servers
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ConnState State
        {
            get
            {
                return _status;
            }
        }

        public void Close()
        {
            _status = ConnState.CLOSED;
            conn.teardown();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void Flush(int timeout)
        {
            throw new NotImplementedException();
        }

        public bool IsClosed()
        {
            return (_status == ConnState.CLOSED);
        }


        public string NewInbox()
        {
            throw new NotImplementedException();
        }

        public void Publish(Msg msg)
        {
            throw new NotImplementedException();
        }

        public void Publish(string subject, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void Publish(string subject, string reply, byte[] data)
        {
            throw new NotImplementedException();
        }

        public Msg Request(string subject, byte[] data)
        {
            throw new NotImplementedException();
        }

        public Msg Request(string subject, byte[] data, int timeout)
        {
            throw new NotImplementedException();
        }

        public ISyncSubscription SubscribeSync(string subject)
        {
            throw new NotImplementedException();
        }

        public ISyncSubscription SubscribeSync(string subject, string queue)
        {
            throw new NotImplementedException();
        }


        internal virtual void RemoveSub(Subscription s)
        {
            Subscription o;

            _subs.TryRemove(s.sid, out o);
            s.Close();
        }

        internal void Unsubscribe(Subscription sub, int max)
        {
            lock (_mu)
            {
                if (IsClosed())
                    throw new FastNATSConnectionClosedException();

                Subscription s = _subs[sub.sid];
                if (s == null)
                {
                    // already unsubscribed
                    return;
                }

                if (max > 0)
                {
                    s.max = max;
                }
                else
                {
                    RemoveSub(s);
                }

                // We will send all subscriptions when reconnecting
                // so that we can supress here.
                if (!IsReconnecting())
                    WriteString(IC.unsubProto, s.sid, max);
            }

        }

        private void WriteString(string format, object a, object b)
        {
            WriteString(String.Format(format, a, b));
        }

        private void WriteString(string format, object a, object b, object c)
        {
            WriteString(String.Format(format, a, b, c));
        }

        private void WriteString(string value)
        {
            byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(value);
            _bw.Write(sendBytes, 0, sendBytes.Length);
        }

        public bool IsConnecting()
        {
            return (_status == ConnState.CONNECTING);
        }

        public bool IsReconnecting()
        {
            return (_status == ConnState.RECONNECTING);
        }

    }
}

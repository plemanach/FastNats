using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FastNats.Client
{
    internal sealed class TCPConnection
    {
        /// A note on the use of streams.  .NET provides a BufferedStream
        /// that can sit on top of an IO stream, in this case the network
        /// stream. It increases performance by providing an additional
        /// buffer.
        /// 
        /// So, here's what we have for writing:
        ///     Client code
        ///          ->BufferedStream (bw)
        ///              ->NetworkStream/SslStream (srvStream)
        ///                  ->TCPClient (srvClient);
        ///                  
        ///  For reading:
        ///     Client code
        ///          ->NetworkStream/SslStream (srvStream)
        ///              ->TCPClient (srvClient);
        /// 
        Object mu = new Object();
        TcpClient client = null;
        NetworkStream stream = null;
        SslStream sslStream = null;

        string hostName = null;

        internal void open(Srv s, int timeoutMillis)
        {
            lock (mu)
            {
                // If a connection was lost during a reconnect we 
                // we could have a defunct SSL stream remaining and 
                // need to clean up.
                if (sslStream != null)
                {
                    try
                    {
                        sslStream.Dispose();
                    }
                    catch (Exception) { }
                    sslStream = null;
                }

#if NET45
                    client = new TcpClient(s.url.Host, s.url.Port);
#else
                client = new TcpClient();
                if (!client.ConnectAsync(s.url.Host, s.url.Port).Wait(TimeSpan.FromMilliseconds(timeoutMillis)))
                {
                    throw new FastNATSConnectionException("timeout");
                }
#endif

                client.NoDelay = false;

                client.ReceiveBufferSize = Defaults.defaultBufSize * 2;
                client.SendBufferSize = Defaults.defaultBufSize;

                stream = client.GetStream();

                // save off the hostname
                hostName = s.url.Host;
            }
        }

        private static bool remoteCertificateValidation(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            return false;
        }

        internal void closeClient(TcpClient c)
        {
            if (c != null)
            {
#if NET45
                    c.Close();
#else
                c.Dispose();
#endif
            }
        }


        internal int ConnectTimeout
        {
            set
            {
                ConnectTimeout = value;
            }
        }

        internal int SendTimeout
        {
            set
            {
                if (client != null)
                    client.SendTimeout = value;
            }
        }

        internal bool isSetup()
        {
            return (client != null);
        }

        internal void teardown()
        {
            TcpClient c;
            Stream s;

            lock (mu)
            {
                c = client;
                s = getReadBufferedStream();

                client = null;
                stream = null;
                sslStream = null;
            }

            try
            {
                if (s != null)
                    s.Dispose();

                if (c != null)
                    closeClient(c);
            }
            catch (Exception) { }
        }

        internal Stream getReadBufferedStream()
        {
            if (sslStream != null)
                return sslStream;

            return stream;
        }

        internal Stream getWriteBufferedStream(int size)
        {
            BufferedStream bs = null;

            if (sslStream != null)
                bs = new BufferedStream(sslStream, size);
            else
                bs = new BufferedStream(stream, size);

            return bs;
        }

        internal bool Connected
        {
            get
            {
                if (client == null)
                    return false;

                return client.Connected;
            }
        }

        internal bool DataAvailable
        {
            get
            {
                if (stream == null)
                    return false;

                return stream.DataAvailable;
            }
        }
    }
}

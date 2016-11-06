using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace FastNats.Client
{
    [DataContract]
    internal class ServerInfo
    {
        internal string serverId;
        internal string serverHost;
        internal int serverPort;
        internal string serverVersion;
        internal bool authRequired;
        internal bool tlsRequired;
        internal long maxPayload;
        internal string[] connectURLs;

        [DataMember]
        public string server_id
        {
            get { return serverId; }
            set { serverId = value; }
        }

        [DataMember]
        public string host
        {
            get { return serverHost; }
            set { serverHost = value; }
        }

        [DataMember]
        public int port
        {
            get { return serverPort; }
            set { serverPort = value; }
        }

        [DataMember]
        public string version
        {
            get { return serverVersion; }
            set { serverVersion = value; }
        }

        [DataMember]
        public bool auth_required
        {
            get { return authRequired; }
            set { authRequired = value; }
        }

        [DataMember]
        public bool tls_required
        {
            get { return tlsRequired; }
            set { tlsRequired = value; }
        }

        [DataMember]
        public long max_payload
        {
            get { return maxPayload; }
            set { maxPayload = value; }
        }

        [DataMember]
        public string[] connect_urls
        {
            get { return connectURLs; }
            set { connectURLs = value; }
        }

        public static ServerInfo CreateFromJson(string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(ServerInfo));
                stream.Position = 0;
                return (ServerInfo)serializer.ReadObject(stream);
            }
        }
    }
}

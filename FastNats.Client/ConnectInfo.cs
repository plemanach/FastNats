using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace FastNats.Client
{
    internal enum ClientProtcolVersion
    {
        // clientProtoZero is the original client protocol from 2009.
        // http://nats.io/documentation/internals/nats-protocol/
        ClientProtoZero = 0,

        // ClientProtoInfo signals a client can receive more then the original INFO block.
        // This can be used to update clients on other cluster members, etc.
        ClientProtoInfo
    }

    [DataContract]
    internal class ConnectInfo
    {
        bool isVerbose;
        bool isPedantic;
        string clientUser;
        string clientPass;
        bool sslRequired;
        string clientName;
        string clientLang = Defaults.LangString;
        string clientVersion = Defaults.Version;
        int protocolVersion = (int)ClientProtcolVersion.ClientProtoInfo;
        string authToken = null;

        [DataMember]
        public bool verbose
        {
            get { return isVerbose; }
            set { isVerbose = value; }
        }

        [DataMember]
        public bool pedantic
        {
            get { return isPedantic; }
            set { isPedantic = value; }
        }

        [DataMember]
        public string user
        {
            get { return clientUser; }
            set { clientUser = value; }
        }

        [DataMember]
        public string pass
        {
            get { return clientPass; }
            set { clientPass = value; }
        }

        [DataMember]
        public bool ssl_required
        {
            get { return sslRequired; }
            set { sslRequired = value; }
        }

        [DataMember]
        public string name
        {
            get { return clientName; }
            set { clientName = value; }
        }

        [DataMember]
        public string auth_token
        {
            get { return authToken; }
            set { authToken = value; }
        }

        [DataMember]
        public string lang
        {
            get { return clientLang; }
            set { clientLang = value; }
        }

        [DataMember]
        public string version
        {
            get { return clientVersion; }
            set { clientVersion = value; }
        }

        [DataMember]
        public int protocol
        {
            get { return protocolVersion; }
            set { protocolVersion = value; }
        }

        internal ConnectInfo(bool verbose, bool pedantic, string user, string pass,
            string token, bool secure, string name)
        {
            isVerbose = verbose;
            isPedantic = pedantic;
            clientUser = user;
            clientPass = pass;
            sslRequired = secure;
            clientName = name;
            authToken = token;
        }

        internal string ToJson()
        {
            var serializer = new DataContractJsonSerializer(typeof(ConnectInfo));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}

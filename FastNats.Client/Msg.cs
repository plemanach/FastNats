using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastNats.Client
{
    /// <summary>
    /// A NATS message is an object encapsulating a subject, optional reply
    /// payload, and subscription information, sent or received by teh client
    /// application.
    /// </summary>
    public sealed class Msg
    {
        private string _subject;
        private string _reply;
        private byte[] _data;
        private long _sid;
        /// <summary>
        /// Creates an empty message.
        /// </summary>
        public Msg()
        {
            _subject = null;
            _reply = null;
            _data = null;
        }

        private void init(string subject, string reply, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException(
                    "Subject cannot be null, empty, or whitespace.",
                    "subject");
            }

            this.Subject = subject;
            this.Reply = reply;
            this.Data = data;
        }

        /// <summary>
        /// Creates a message with a subject, reply, and data.
        /// </summary>
        /// <param name="subject">Subject of the message, required.</param>
        /// <param name="reply">Reply subject, can be null.</param>
        /// <param name="data">Message payload</param>
        public Msg(string subject, string reply, byte[] data)
        {
            init(subject, reply, data);
        }

        /// <summary>
        /// Creates a message with a subject and data.
        /// </summary>
        /// <param name="subject">Subject of the message, required.</param>
        /// <param name="data">Message payload</param>
        public Msg(string subject, byte[] data)
        {
            init(subject, null, data);
        }

        /// <summary>
        /// Creates a message with a subject and no payload.
        /// </summary>
        /// <param name="subject">Subject of the message, required.</param>
        public Msg(string subject)
        {
            init(subject, null, null);
        }

        internal Msg(MsgArg arg, byte[] payload, long length)
        {
            _subject = arg.subject;
            _reply = arg.reply;
            _sid = arg.sid;


            // make a deep copy of the bytes for this message.
            _data = new byte[length];
            Array.Copy(payload, _data, (int)length);
        }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        public string Subject
        {
            get { return _subject; }
            set { _subject = value; }
        }

        /// <summary>
        /// Gets or sets the reply subject.
        /// </summary>
        public string Reply
        {
            get { return _reply; }
            set { _reply = value; }
        }

        /// <summary>
        /// Gets or sets the sid.
        /// </summary>
        public long Sid
        {
            get { return _sid; }
            set { _sid = value; }
        }


        /// <summary>
        /// Sets data in the message.  This copies application data into the message.
        /// </summary>
        /// <remarks>
        /// See <see cref="AssignData">AssignData</see> to directly pass the bytes
        /// buffer.
        /// </remarks>
        /// <see cref="AssignData"/>
        public byte[] Data
        {
            get { return _data; }

            set
            {
                if (value == null)
                {
                    this._data = null;
                    return;
                }

                int len = value.Length;
                if (len == 0)
                    this._data = null;
                else
                {
                    this._data = new byte[len];
                    Array.Copy(value, _data, len);
                }
            }
        }

        /// <summary>
        /// Assigns the data of the message.  This is a direct assignment,
        /// to avoid expensive copy operations.  A change to the passed
        /// byte array will be changed in the message.
        /// </summary>
        /// <remarks>
        /// The application is responsible for the data integrity in the message.
        /// </remarks>
        /// <param name="data">a bytes buffer of data.</param>
        public void AssignData(byte[] data)
        {
            this._data = data;
        }
        
        /// <summary>
        /// Generates a string representation of the messages.
        /// </summary>
        /// <returns>A string representation of the messages.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("Subject={0};Reply={1};Payload=<", Subject,
                Reply != null ? _reply : "null");

            int len = _data.Length;
            int i;

            for (i = 0; i < 32 && i < len; i++)
            {
                sb.Append((char)_data[i]);
            }

            if (i < len)
            {
                sb.AppendFormat("{0} more bytes", len - i);
            }

            sb.Append(">}");

            return sb.ToString();
        }
    }
}

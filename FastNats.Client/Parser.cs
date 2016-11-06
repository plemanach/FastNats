using System;
using System.IO;

namespace FastNats.Client
{
    internal sealed class Parser
    {
        byte[] _argBufBase = new byte[32768];
        MemoryStream _argBufStream = null;

        byte[] _msgBufBase = new byte[32768];
        MemoryStream _msgBufStream = null;

        MsgArg _msgArgs = new MsgArg();

        public event EventHandler<string> ErrorRaised;
        public event EventHandler<Msg> MsgReceived;
        public event EventHandler PingReceived;

        internal Parser()
        {
            _argBufStream = new MemoryStream(_argBufBase);
            _msgBufStream = new MemoryStream(_msgBufBase);
        }

        internal int state = 0;

        private const int MAX_CONTROL_LINE_SIZE = 1024;

        // For performance declare these as consts - they'll be
        // baked into the IL code (thus faster).  An enum would
        // be nice, but we want speed in this critical section of
        // message handling.
        private const int OP_START = 0;
        private const int OP_PLUS = 1;
        private const int OP_PLUS_O = 2;
        private const int OP_PLUS_OK = 3;
        private const int OP_MINUS = 4;
        private const int OP_MINUS_E = 5;
        private const int OP_MINUS_ER = 6;
        private const int OP_MINUS_ERR = 7;
        private const int OP_MINUS_ERR_SPC = 8;
        private const int MINUS_ERR_ARG = 9;
        private const int OP_C = 10;
        private const int OP_CO = 11;
        private const int OP_CON = 12;
        private const int OP_CONN = 13;
        private const int OP_CONNE = 14;
        private const int OP_CONNEC = 15;
        private const int OP_CONNECT = 16;
        private const int CONNECT_ARG = 17;
        private const int OP_M = 18;
        private const int OP_MS = 19;
        private const int OP_MSG = 20;
        private const int OP_MSG_SPC = 21;
        private const int MSG_ARG = 22;
        private const int MSG_PAYLOAD = 23;
        private const int MSG_END = 24;
        private const int OP_P = 25;
        private const int OP_PI = 26;
        private const int OP_PIN = 27;
        private const int OP_PING = 28;
        private const int OP_PO = 29;
        private const int OP_PON = 30;
        private const int OP_PONG = 31;
        private const int OP_I = 32;
        private const int OP_IN = 33;
        private const int OP_INF = 34;
        private const int OP_INFO = 35;
        private const int OP_INFO_SPC = 36;
        private const int INFO_ARG = 37;

        private void parseError(byte[] buffer, int position)
        {
            throw new Exception(string.Format("Parse Error [{0}], {1}", state, buffer));
        }

        internal void OnErrorRaised(string error)
        {
            var handler = ErrorRaised;

            if (handler != null)
            {
                handler(this, error);
            }
        }

        internal void OnPingReceived()
        {
            var handler = PingReceived;

            if (handler != null)
            {
                handler(this, null);
            }
        }

        internal void OnMsgReceived(Msg msg)
        {
            var handler = MsgReceived;

            if (handler != null)
            {
                handler(this, msg);
            }
        }

        internal void parse(byte[] buffer, int len)
        {
            MsgArg msgArgs = null;
            int i;
            char b;

            for (i = 0; i < len; i++)
            {
                b = (char)buffer[i];

                switch (state)
                {
                    case OP_START:
                        switch (b)
                        {
                            case 'M':
                            case 'm':
                                state = OP_M;
                                break;
                            case 'C':
                            case 'c':
                                state = OP_C;
                                break;
                            case 'P':
                            case 'p':
                                state = OP_P;
                                break;
                            case '+':
                                state = OP_PLUS;
                                break;
                            case '-':
                                state = OP_MINUS;
                                break;
                            case 'i':
                            case 'I':
                                state = OP_I;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_M:
                        switch (b)
                        {
                            case 'S':
                            case 's':
                                state = OP_MS;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_MS:
                        switch (b)
                        {
                            case 'G':
                            case 'g':
                                state = OP_MSG;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_MSG:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                state = OP_MSG_SPC;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_MSG_SPC:
                        switch (b)
                        {
                            case ' ':
                                break;
                            case '\t':
                                break;
                            default:
                                state = MSG_ARG;
                                i--;
                                break;
                        }
                        break;
                    case MSG_ARG:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                msgArgs = processMsgArgs(_argBufBase, _argBufStream.Position);
                                _argBufStream.Position = 0;
                                if (msgArgs.size > _msgBufBase.Length)
                                {
                                    // Add 2 to account for the \r\n
                                    _msgBufBase = new byte[msgArgs.size + 2];
                                    _msgBufStream = new MemoryStream(_msgBufBase);
                                }
                                state = MSG_PAYLOAD;
                                break;
                            default:
                                _argBufStream.WriteByte((byte)b);
                                break;
                        }
                        break;
                    case MSG_PAYLOAD:
                        int msgSize = msgArgs.size;
                        if (msgSize == 0)
                        {
                            processMsg(_msgBufBase, msgSize);
                            state = MSG_END;
                        }
                        else
                        {
                            long position = _msgBufStream.Position;
                            int writeLen = msgSize - (int)position;
                            int avail = len - i;

                            if (avail < writeLen)
                            {
                                writeLen = avail;
                            }

                            _msgBufStream.Write(buffer, i, writeLen);
                            i += (writeLen - 1);

                            if ((position + writeLen) >= msgSize)
                            {
                                processMsg(_msgBufBase, msgSize);
                                _msgBufStream.Position = 0;
                                state = MSG_END;
                            }
                        }
                        break;
                    case MSG_END:
                        switch (b)
                        {
                            case '\n':
                                state = OP_START;
                                break;
                            default:
                                continue;
                        }
                        break;
                    case OP_PLUS:
                        switch (b)
                        {
                            case 'O':
                            case 'o':
                                state = OP_PLUS_O;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_PLUS_O:
                        switch (b)
                        {
                            case 'K':
                            case 'k':
                                state = OP_PLUS_OK;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_PLUS_OK:
                        switch (b)
                        {
                            case '\n':
                                //conn.processOK();
                                state = OP_START;
                                break;
                        }
                        break;
                    case OP_MINUS:
                        switch (b)
                        {
                            case 'E':
                            case 'e':
                                state = OP_MINUS_E;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_MINUS_E:
                        switch (b)
                        {
                            case 'R':
                            case 'r':
                                state = OP_MINUS_ER;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_MINUS_ER:
                        switch (b)
                        {
                            case 'R':
                            case 'r':
                                state = OP_MINUS_ERR;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_MINUS_ERR:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                state = OP_MINUS_ERR_SPC;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_MINUS_ERR_SPC:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                state = OP_MINUS_ERR_SPC;
                                break;
                            default:
                                state = MINUS_ERR_ARG;
                                i--;
                                break;
                        }
                        break;
                    case MINUS_ERR_ARG:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                //TODO:Error raised
                                OnErrorRaised("");
                                //conn.processErr(argBufStream);
                                _argBufStream.Position = 0;
                                state = OP_START;
                                break;
                            default:
                                _argBufStream.WriteByte((byte)b);
                                break;
                        }
                        break;
                    case OP_P:
                        switch (b)
                        {
                            case 'I':
                            case 'i':
                                state = OP_PI;
                                break;
                            case 'O':
                            case 'o':
                                state = OP_PO;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_PO:
                        switch (b)
                        {
                            case 'N':
                            case 'n':
                                state = OP_PON;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_PON:
                        switch (b)
                        {
                            case 'G':
                            case 'g':
                                state = OP_PONG;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_PONG:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                //TODO:implement pong
                                //conn.processPong();
                                state = OP_START;
                                break;
                        }
                        break;
                    case OP_PI:
                        switch (b)
                        {
                            case 'N':
                            case 'n':
                                state = OP_PIN;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_PIN:
                        switch (b)
                        {
                            case 'G':
                            case 'g':
                                state = OP_PING;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_PING:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                OnPingReceived();
                                state = OP_START;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_I:
                        switch (b)
                        {
                            case 'N':
                            case 'n':
                                state = OP_IN;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_IN:
                        switch (b)
                        {
                            case 'F':
                            case 'f':
                                state = OP_INF;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_INF:
                        switch (b)
                        {
                            case 'O':
                            case 'o':
                                state = OP_INFO;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_INFO:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                state = OP_INFO_SPC;
                                break;
                            default:
                                parseError(buffer, i);
                                break;
                        }
                        break;
                    case OP_INFO_SPC:
                        switch (b)
                        {
                            case ' ':
                            case '\t':
                                break;
                            default:
                                _argBufStream.Position = 0;
                                state = INFO_ARG;
                                i--;
                                break;
                        }
                        break;
                    case INFO_ARG:
                        switch (b)
                        {
                            case '\r':
                                break;
                            case '\n':
                                //TODO:implement info
                                //conn.processAsyncInfo(argBufBase, (int)argBufStream.Position);
                                state = OP_START;
                                break;
                            default:
                                _argBufStream.WriteByte((byte)b);
                                break;
                        }
                        break;
                    default:
                        throw new FastNATSException("Unable to parse.");
                } // switch(state)

            }  // for
        } // parse



        // Roll our own fast conversion - we know it's the right
        // encoding. 
        char[] convertToStrBuf = new char[Defaults.scratchSize];

        // Caller must ensure thread safety.
        private string convertToString(byte[] buffer, long length)
        {
            // expand if necessary
            if (length > convertToStrBuf.Length)
            {
                convertToStrBuf = new char[length];
            }

            for (int i = 0; i < length; i++)
            {
                convertToStrBuf[i] = (char)buffer[i];
            }

            // This is the copy operation for msg arg strings.
            return new String(convertToStrBuf, 0, (int)length);
        }

        internal MsgArg processMsgArgs(byte[] buffer, long length)
        {
            string s = convertToString(buffer, length);
            string[] args = s.Split(' ');

            MsgArg msgArgs = new MsgArg();

            switch (args.Length)
            {
                case 3:
                    msgArgs.subject = args[0];
                    msgArgs.sid = LongParseFast(args[1]);
                    msgArgs.reply = null;
                    msgArgs.size = IntParseFast(args[2]);
                    break;
                case 4:
                    msgArgs.subject = args[0];
                    msgArgs.sid = LongParseFast(args[1]);
                    msgArgs.reply = args[2];
                    msgArgs.size = IntParseFast(args[3]);
                    break;
                default:
                    throw new FastNATSException("Unable to parse message arguments: " + s);
            }

            if (msgArgs.size < 0)
            {
                throw new FastNATSException("Invalid Message - Bad or Missing Size: " + s);
            }
            if (msgArgs.sid < 0)
            {
                throw new FastNATSException("Invalid Message - Bad or Missing Sid: " + s);
            }
            return msgArgs;
        }

        // processMsg is called by parse and will place the msg on the
        // appropriate channel for processing. All subscribers have their
        // their own channel. If the channel is full, the connection is
        // considered a slow subscriber.
        private Msg processMsg(byte[] msg, long length)
        {
            return new Msg(_msgArgs, msg, length);
        }

        private static int IntParseFast(string value)
        {
            int result = 0;
            int length = value.Length;
            for (int i = 0; i < length; i++)
            {
                result = 10 * result + (value[i] - 48);
            }
            return result;
        }


        private static long LongParseFast(string value)
        {
            long result = 0;
            int length = value.Length;
            for (int i = 0; i < length; i++)
            {
                result = 10 * result + (value[i] - 48);
            }
            return result;
        }
    }
}

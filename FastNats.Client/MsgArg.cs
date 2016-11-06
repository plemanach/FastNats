using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastNats.Client
{
    internal sealed class MsgArg
    {
        internal string subject;
        internal string reply;
        internal long sid;
        internal int size;
    }

}

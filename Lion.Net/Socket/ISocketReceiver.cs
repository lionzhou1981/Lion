using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lion.Net.Sockets
{
    public interface ISocketReceiver<T>
    {
        void ReceivedCommand(T _package);
    }
}

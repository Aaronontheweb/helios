using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Helios.Util;

namespace Helios.FsCheck.Tests.Channels.Sockets
{
    public interface ITcpServerSocketModel
    {
        IPEndPoint BoundAddress { get; }
        IReadOnlyList<IPEndPoint> Clients { get; }
        IReadOnlyList<int> LastReceivedMessages { get; }

        IReadOnlyList<int> WrittenMessages { get; }

        ITcpServerSocketModel SetAddress(IPEndPoint boundAddress);
        ITcpServerSocketModel AddClient(IPEndPoint endpoint);
        ITcpServerSocketModel RemoveClient(IPEndPoint endpoint);
        ITcpServerSocketModel ClearMessages();
        ITcpServerSocketModel WriteMessages(params int[] messages);
        ITcpServerSocketModel ReceiveMessages(params int[] messages);
    }
}

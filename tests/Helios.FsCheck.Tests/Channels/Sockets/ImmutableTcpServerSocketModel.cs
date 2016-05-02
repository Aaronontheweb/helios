using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Helios.FsCheck.Tests.Channels.Sockets
{
    public sealed class ImmutableTcpServerSocketModel : ITcpServerSocketModel
    {
        private static readonly IReadOnlyList<IPEndPoint> EmptyIps = new List<IPEndPoint>();
        private static readonly IReadOnlyList<int> EmptyMessages = new List<int>();

        public ImmutableTcpServerSocketModel() : this(null) { }

        public ImmutableTcpServerSocketModel(IPEndPoint boundAddress)
            : this(boundAddress, EmptyIps, EmptyMessages, EmptyMessages)
        { }

        public ImmutableTcpServerSocketModel(IPEndPoint boundAddress, IReadOnlyList<IPEndPoint> clients, IReadOnlyList<int> lastReceivedMessages, IReadOnlyList<int> writtenMessages)
        {
            BoundAddress = boundAddress;
            Clients = clients;
            LastReceivedMessages = lastReceivedMessages;
            WrittenMessages = writtenMessages;
        }

        public IPEndPoint BoundAddress { get; }
        public IReadOnlyList<IPEndPoint> Clients { get; }
        public IReadOnlyList<int> LastReceivedMessages { get; }
        public IReadOnlyList<int> WrittenMessages { get; }
        public ITcpServerSocketModel SetAddress(IPEndPoint boundAddress)
        {
            return new ImmutableTcpServerSocketModel(boundAddress, Clients, LastReceivedMessages, WrittenMessages);
        }

        public ITcpServerSocketModel AddClient(IPEndPoint endpoint)
        {
            return new ImmutableTcpServerSocketModel(BoundAddress, Clients.Concat(new[] { endpoint}).ToList(), LastReceivedMessages, WrittenMessages);
        }

        public ITcpServerSocketModel RemoveClient(IPEndPoint endpoint)
        {
            return new ImmutableTcpServerSocketModel(BoundAddress, Clients.Where(x => !Equals(x, endpoint)).ToList(), LastReceivedMessages, WrittenMessages);
        }

        public ITcpServerSocketModel ClearMessages()
        {
            return new ImmutableTcpServerSocketModel(BoundAddress, Clients, EmptyMessages, EmptyMessages);
        }

        public ITcpServerSocketModel WriteMessages(params int[] messages)
        {
            return new ImmutableTcpServerSocketModel(BoundAddress, Clients, LastReceivedMessages, WrittenMessages.Concat(messages).ToList());
        }

        public ITcpServerSocketModel ReceiveMessages(params int[] messages)
        {
            return new ImmutableTcpServerSocketModel(BoundAddress, Clients, LastReceivedMessages.Concat(messages).ToList(), WrittenMessages);
        }
    }
}
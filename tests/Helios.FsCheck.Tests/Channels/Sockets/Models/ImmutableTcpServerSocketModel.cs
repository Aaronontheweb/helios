using System.Collections.Generic;
using System.Linq;
using System.Net;
using Helios.Channels;

namespace Helios.FsCheck.Tests.Channels.Sockets.Models
{
    public sealed class ImmutableTcpServerSocketModel : ITcpServerSocketModel
    {
        private static readonly IReadOnlyList<IPEndPoint> EmptyIps = new List<IPEndPoint>();
        private static readonly IReadOnlyList<IChannel> EmptyChannels = new List<IChannel>(); 
        private static readonly IReadOnlyList<int> EmptyMessages = new List<int>();

        public ImmutableTcpServerSocketModel() : this(null) { }

        public ImmutableTcpServerSocketModel(IChannel self)
            : this(self, EmptyIps, EmptyMessages, EmptyMessages, EmptyChannels)
        { }

        public ImmutableTcpServerSocketModel(IChannel self, IReadOnlyList<IPEndPoint> remoteClients, IReadOnlyList<int> lastReceivedMessages, IReadOnlyList<int> writtenMessages, IReadOnlyList<IChannel> localChannels)
        {
            Self = self;
            RemoteClients = remoteClients;
            LastReceivedMessages = lastReceivedMessages;
            WrittenMessages = writtenMessages;
            LocalChannels = localChannels;
            BoundAddress = Self?.LocalAddress as IPEndPoint;
        }

        public IPEndPoint BoundAddress { get; private set; }
        public IChannel Self { get; private set; }
        public IReadOnlyList<IChannel> LocalChannels { get; }
        public IReadOnlyList<IPEndPoint> RemoteClients { get; }
        public IReadOnlyList<int> LastReceivedMessages { get; }
        public IReadOnlyList<int> WrittenMessages { get; }


        /// <summary>
        /// MUTABLE, due to weird setup issue on bind.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public ITcpServerSocketModel SetSelf(IChannel self)
        {
            Self = self;
            return this;
        }

        public ITcpServerSocketModel SetOwnAddress(IPEndPoint endpoint)
        {
            BoundAddress = endpoint;
            return this;
        }

        public ITcpServerSocketModel AddLocalChannel(IChannel channel)
        {
            return new ImmutableTcpServerSocketModel(Self, RemoteClients, LastReceivedMessages, WrittenMessages, LocalChannels.Concat(new[] { channel }).ToList());
        }

        public ITcpServerSocketModel RemoveLocalChannel(IChannel channel)
        {
            return new ImmutableTcpServerSocketModel(Self, RemoteClients, LastReceivedMessages, WrittenMessages, LocalChannels.Where(x => !x.Id.Equals(channel.Id)).ToList());
        }

        public ITcpServerSocketModel AddClient(IPEndPoint endpoint)
        {
            return new ImmutableTcpServerSocketModel(Self, RemoteClients.Concat(new[] { endpoint}).ToList(), LastReceivedMessages, WrittenMessages, LocalChannels);
        }

        public ITcpServerSocketModel RemoveClient(IPEndPoint endpoint)
        {
            return new ImmutableTcpServerSocketModel(Self, RemoteClients.Where(x => !Equals(x, endpoint)).ToList(), LastReceivedMessages, WrittenMessages, LocalChannels);
        }

        public ITcpServerSocketModel ClearMessages()
        {
            return new ImmutableTcpServerSocketModel(Self, RemoteClients, EmptyMessages, EmptyMessages, LocalChannels);
        }

        public ITcpServerSocketModel WriteMessages(params int[] messages)
        {
            return new ImmutableTcpServerSocketModel(Self, RemoteClients, LastReceivedMessages, WrittenMessages.Concat(messages).ToList(), LocalChannels);
        }

        public ITcpServerSocketModel ReceiveMessages(params int[] messages)
        {
            return new ImmutableTcpServerSocketModel(Self, RemoteClients, LastReceivedMessages.Concat(messages).ToList(), WrittenMessages, LocalChannels);
        }

        public override string ToString()
        {
            return
                $"TcpServerState(BoundAddress={BoundAddress}, Active={Self?.IsActive ?? false} RemoteConnections=[{string.Join("|", RemoteClients)}], Written=[{string.Join(",", WrittenMessages)}], Received=[{string.Join(",", LastReceivedMessages)}])";
        }
    }
}
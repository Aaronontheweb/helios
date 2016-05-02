using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Helios.Channels.Sockets;

namespace Helios.FsCheck.Tests.Channels.Sockets
{
    /// <summary>
    /// Shared implementation of <see cref="ITcpServerSocketModel"/> that is used across multiple child
    /// <see cref="TcpSocketChannel"/> instances spawned by a single <see cref="TcpServerSocketChannel"/>
    /// </summary>
    public class ConcurrentTcpServerSocketModel : ITcpServerSocketModel
    {
        public ConcurrentTcpServerSocketModel(IPEndPoint boundAddress)
        {
            BoundAddress = boundAddress;
        }

        public IPEndPoint BoundAddress { get; }

        private readonly List<IPEndPoint> _clients = new List<IPEndPoint>();
        public IReadOnlyList<IPEndPoint> Clients => _clients;

        private ConcurrentBag<int> _receivedMessages = new ConcurrentBag<int>();
        public IReadOnlyList<int> LastReceivedMessages => _receivedMessages.ToList();

        private ConcurrentBag<int> _writtenMessages = new ConcurrentBag<int>();
        public IReadOnlyList<int> WrittenMessages => _writtenMessages.ToList();
        public ITcpServerSocketModel AddClient(IPEndPoint endpoint)
        {
            lock (_clients)
            {
                _clients.Add(endpoint);
            }
            return this;
        }

        public ITcpServerSocketModel RemoveClient(IPEndPoint endpoint)
        {
            lock (_clients)
            {
                _clients.Remove(endpoint);
            }
            return this;
        }

        public ITcpServerSocketModel ClearMessages()
        {
            _receivedMessages = new ConcurrentBag<int>();
            _writtenMessages = new ConcurrentBag<int>();
            return this;
        }

        public ITcpServerSocketModel WriteMessages(params int[] messages)
        {
            foreach(var message in messages)
                _writtenMessages.Add(message);
            return this;
        }

        public ITcpServerSocketModel ReceiveMessages(params int[] messages)
        {
            foreach(var message in messages)
                _receivedMessages.Add(message);
            return this;
        }
    }
}
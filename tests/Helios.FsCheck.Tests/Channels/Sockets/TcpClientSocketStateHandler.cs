using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Helios.Channels;

namespace Helios.FsCheck.Tests.Channels.Sockets
{
    public sealed class TcpClientSocketStateHandler : ChannelHandlerAdapter
    {
        public TcpClientSocketStateHandler() : this(new TcpClientSocketModel())
        {
        }

        public TcpClientSocketStateHandler(TcpClientSocketModel state)
        {
            State = state;
        }

        public TcpClientSocketModel State { get; private set; }

        public override Task ConnectAsync(IChannelHandlerContext context, EndPoint remoteAddress, EndPoint localAddress)
        {
            State = State.SetChannel(context.Channel).SetState(ConnectionState.Connecting);
            return base.ConnectAsync(context, remoteAddress, localAddress);
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            State = State.SetChannel(context.Channel).SetState(ConnectionState.Active);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            State = State.SetState(ConnectionState.Shutdown);
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            if (message is int)
            {
                State = State.WriteMessages((int) message);
            }
            return context.WriteAsync(message);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is int)
            {
                State = State.ReceiveMessages((int) message);
            }
        }
    }
}

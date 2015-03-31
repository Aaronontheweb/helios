using System;
using Helios.Tracing;
using Microsoft.Diagnostics.Tracing;

namespace Helios.ETW
{
    [EventSource(Name = "HeliosTrace")]
    public sealed class HeliosEtwTraceWriter : EventSource, IHeliosTraceWriter
    {
        public void TcpClientConnectSuccess()
        {
            WriteEvent(1);
        }

        public void TcpClientConnectFailure(string reason)
        {
            WriteEvent(2, reason);
        }

        public void TcpClientSend(int payloadLength)
        {
            WriteEvent(3, payloadLength);
        }

        public void TcpClientSendSuccess()
        {
            WriteEvent(4);
        }

        public void TcpClientSendFailure()
        {
            WriteEvent(5);
        }

        public void TcpClientReceive(int payloadLength)
        {
            WriteEvent(6, payloadLength);
        }

        public void TcpClientReceiveSuccess()
        {
            WriteEvent(7);
        }

        public void TcpClientReceiveFailure()
        {
            WriteEvent(8);
        }

        public void TcpInboundAcceptSuccess()
        {
            WriteEvent(9);
        }

        public void TcpInboundAcceptFailure(string reason)
        {
            WriteEvent(10, reason);
        }

        public void TcpInboundClientSend(int payloadLength)
        {
            WriteEvent(11, payloadLength);
        }

        public void TcpInboundSendSuccess()
        {
            WriteEvent(12);
        }

        public void TcpInboundSendFailure()
        {
            WriteEvent(13);
        }

        public void TcpInboundReceive(int payloadLength)
        {
            WriteEvent(14, payloadLength);
        }

        public void TcpInboundReceiveSuccess()
        {
            WriteEvent(15);
        }

        public void TcpInboundReceiveFailure()
        {
            WriteEvent(16);
        }

        public void UdpClientSend(int payloadLength)
        {
            WriteEvent(17);
        }

        public void UdpClientSendSuccess()
        {
            WriteEvent(18);
        }

        public void UdpClientSendFailure()
        {
            WriteEvent(19);
        }

        public void UdpClientReceive(int payloadLength)
        {
            WriteEvent(20);
        }

        public void UdpClientReceiveSuccess()
        {
            WriteEvent(21);
        }

        public void UdpClientReceiveFailure()
        {
            WriteEvent(22);
        }

        public void DecodeSucccess(int messageCount)
        {
            WriteEvent(23, messageCount);
        }

        public void DecodeFailure()
        {
            WriteEvent(24);
        }

        public void DecodeMalformedBytes(int byteCount)
        {
            WriteEvent(25, byteCount);
        }

        public void EncodeSuccess()
        {
            WriteEvent(26);
        }

        public void EncodeFailure()
        {
            WriteEvent(27);
        }
    }
}

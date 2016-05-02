using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Experimental;
using Helios.Channels;
using Helios.Channels.Bootstrap;
using Helios.Channels.Sockets;
using Helios.Codecs;
using Helios.FsCheck.Tests.Channels.Sockets.Models;
using Helios.Tests.Channels;
using Helios.Util;
using static Helios.FsCheck.Tests.HeliosModelHelpers;

namespace Helios.FsCheck.Tests.Channels.Sockets
{
    public class TcpServerSocketChannelStateMachine : Machine<ITcpServerSocketModel, ITcpServerSocketModel>
    {
        public override Gen<Operation<ITcpServerSocketModel, ITcpServerSocketModel>> Next(ITcpServerSocketModel obj0)
        {
            if (obj0.Self == null)
            {
                // generate a bind operation first
                return Bind.Generator();
            }
            return Gen.OneOf(ClientConnect.Generator(), ClientWrite.Generator(),
                ClientDisconnect.Generator());
        }

        public override Arbitrary<Setup<ITcpServerSocketModel, ITcpServerSocketModel>> Setup
            =>
                Arb.From(
                    Gen.Fresh(
                        () => (Setup<ITcpServerSocketModel, ITcpServerSocketModel>) new TcpServerSocketChannelSetup()));

        #region Setup

        /// <summary>
        /// We don't perform any actual work on the channel here - that's performed inside separate commands
        /// that have to be activated in an order determined by pre-conditions.
        /// </summary>
        public class TcpServerSocketChannelSetup : Setup<ITcpServerSocketModel, ITcpServerSocketModel>
        {
            public override ITcpServerSocketModel Actual()
            {
                return new ConcurrentTcpServerSocketModel();
            }

            public override ITcpServerSocketModel Model()
            {
                return new ImmutableTcpServerSocketModel();
                
            }
        }

        #endregion

        #region Helpers

        public static IChannelHandler FreshLengthFramePrepender()
        {
            return new LengthFieldPrepender(4, false);
        }

        public static IChannelHandler FreshLengthFrameDecoder()
        {
            return new LengthFieldBasedFrameDecoder(Int32.MaxValue, 0, 4, 0, 4, true);
        }

        public static IChannelHandler NewIntCodec()
        {
            // releasing messages on this iteration, since these tests may run long
            return new IntCodec(true);
        }

        public static IChannelHandler NewServerHandler(ITcpServerSocketModel model)
        {
            return new TcpServerSocketStateHandler(model);
        }

        public static void ConstructServerPipeline(IChannel channelToModify, ITcpServerSocketModel model)
        {
            channelToModify.Pipeline.AddLast(FreshLengthFramePrepender())
                .AddLast(FreshLengthFrameDecoder())
                .AddLast(NewIntCodec())
                .AddLast(NewServerHandler(model));
        }

        public static void ConstructClientPipeline(IChannel channelToModify)
        {
            channelToModify.Pipeline.AddLast(FreshLengthFramePrepender())
                .AddLast(FreshLengthFrameDecoder())
                .AddLast(NewIntCodec());
        }

        #endregion

        #region Generators

        public class ServerEventLoops
        {
            public ServerEventLoops(IEventLoopGroup serverEventLoopGroup, IEventLoopGroup workerEventLoopGroup)
            {
                ServerEventLoopGroup = serverEventLoopGroup;
                WorkerEventLoopGroup = workerEventLoopGroup;
            }

            public IEventLoopGroup ServerEventLoopGroup { get; private set; }
            public IEventLoopGroup WorkerEventLoopGroup { get; private set; }
        }

        public static Arbitrary<ServerEventLoops> GenServerEventLoops()
        {
            return
                Arb.From(
                    Gen.Constant(new ServerEventLoops(new MultithreadEventLoopGroup(1), new MultithreadEventLoopGroup(2))));
        }

        public static Arbitrary<IEventLoopGroup> GenClientEventLoops()
        {
            return Arb.From(Gen.Constant((IEventLoopGroup)new MultithreadEventLoopGroup(2)));
        }

        #endregion

        #region Commands

        /// <summary>
        /// Bind operation - must be run first before any other commands can be run.
        /// </summary>
        public class Bind : Operation<ITcpServerSocketModel, ITcpServerSocketModel>
        {
            public static Gen<Operation<ITcpServerSocketModel, ITcpServerSocketModel>> Generator()
            {
                return GenServerEventLoops().Generator.Select(x => (Operation<ITcpServerSocketModel, ITcpServerSocketModel>)new Bind(x));
            }

            public static IPEndPoint TEST_ADDRESS = new IPEndPoint(IPAddress.IPv6Loopback, 0);
            private readonly ServerEventLoops _eventLoops;

            public IPEndPoint BoundEndpoint { get; private set; }

            public Bind(ServerEventLoops eventLoops)
            {
                BoundEndpoint = TEST_ADDRESS;
                _eventLoops = eventLoops;
            }

            public override Property Check(ITcpServerSocketModel actual, ITcpServerSocketModel model)
            {
                var sb = new ServerBootstrap()
                  .Group(_eventLoops.ServerEventLoopGroup, _eventLoops.WorkerEventLoopGroup)
                  .Channel<TcpServerSocketChannel>()
                  .ChildHandler(new ActionChannelInitializer<TcpSocketChannel>(channel =>
                  {
                      // actual is implemented using a ConcurrentTcpServerSocketModel, shared mutable state between all children.
                      ConstructServerPipeline(channel, actual);
                  }));

                var serverBindTask = sb.BindAsync(BoundEndpoint);
                var timeout = TimeSpan.FromSeconds(2);
                if (!serverBindTask.Wait(timeout))
                {
                    return false.Label($"Bind operation on {BoundEndpoint} failed after {timeout.TotalSeconds} seconds");
                }

                var serveChannel = serverBindTask.Result;
                // perform a self-referential set, so we get a side effect back onto the model
                model = model.SetSelf(serveChannel);
                return
                    model.BoundAddress.Equals(actual.BoundAddress)
                        .Label(
                            $"Expected actual bound address {actual.BoundAddress} to equal model bound address {model.BoundAddress}");
            }

            public override ITcpServerSocketModel Run(ITcpServerSocketModel obj0)
            {
                var portNumber = ThreadLocalRandom.Current.Next(50000, 60000); // find a randomly available port in a range unlikely to have reserved ports
                var tries = 10;
                while (!IsPortAvailable(portNumber))
                {
                    portNumber = ThreadLocalRandom.Current.Next(50000, 60000);
                    if(--tries < 0)
                        throw new InvalidOperationException($"Attempted to find an open port and failed after 10 tries.");
                }
                
                BoundEndpoint = new IPEndPoint(TEST_ADDRESS.Address, portNumber);

                // this one is weird - all of the work has to happen inside the check stage as we need access to the model.
                return ((ImmutableTcpServerSocketModel)obj0).SetOwnAddress(BoundEndpoint);
            }

            public override bool Pre(ITcpServerSocketModel _arg1)
            {
                // channel is only null if we have not bound
                return _arg1.Self == null;
            }

            public override string ToString()
            {
                return $"Bind({BoundEndpoint})";
            }

            /// <summary>
            /// Taken from http://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available
            /// </summary>
            private bool IsPortAvailable(int port)
            {
                bool isAvailable = true;

                // Evaluate current system tcp connections. This is the same information provided
                // by the netstat command line application, just in .Net strongly-typed object
                // form.  We will look through the list, and if our port we would like to use
                // in our TcpClient is occupied, we will set isAvailable to false.
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

                foreach (IPEndPoint endpoint in tcpConnInfoArray)
                {
                    if (endpoint.Port == port)
                    {
                        isAvailable = false;
                        break;
                    }
                }

                return isAvailable;
            }
        }

        public abstract class NonBindCommand : Operation<ITcpServerSocketModel, ITcpServerSocketModel>
        {
            public override bool Pre(ITcpServerSocketModel _arg1)
            {
                // must be bound
                return _arg1.Self != null;
            }
        }

        public class ClientConnect : NonBindCommand
        {
            public static Gen<Operation<ITcpServerSocketModel, ITcpServerSocketModel>> Generator()
            {
                Func<int, IEventLoopGroup, Operation<ITcpServerSocketModel, ITcpServerSocketModel>> producer = (s, e) => new ClientConnect(s,e) as Operation<ITcpServerSocketModel, ITcpServerSocketModel>;
                var fsFunc = FsharpDelegateHelper.Create(producer);
                return Gen.Map2(fsFunc, Gen.Choose(1,10), GenClientEventLoops().Generator);
            }

            public ClientConnect(int clientCount, IEventLoopGroup clientEventLoopGroup)
            {
                ClientCount = clientCount;
                ClientEventLoopGroup = clientEventLoopGroup;
            }

            /// <summary>
            /// The number of clients we're going to attempt to connect simultaneously
            /// </summary>
            public int ClientCount { get; private set; }

            public IEventLoopGroup ClientEventLoopGroup { get; private set; }

            public override Property Check(ITcpServerSocketModel obj0, ITcpServerSocketModel obj1)
            {
                return (TcpServerSocketModelComparer.Instance.Equals(obj0, obj1))
                    .Label(
                        $"Expected {obj1.RemoteClients.Count} total connections, but found only {obj0.RemoteClients.Count}");
            }

            public override ITcpServerSocketModel Run(ITcpServerSocketModel obj0)
            {
                var cb = new ClientBootstrap()
                    .Group(ClientEventLoopGroup)
                    .Channel<TcpSocketChannel>()
                    .Handler(new ActionChannelInitializer<TcpSocketChannel>(ConstructClientPipeline));

                var connectTasks = new List<Task<IChannel>>();
                for (var i = 0; i < ClientCount; i++)
                {
                    connectTasks.Add(cb.ConnectAsync(obj0.BoundAddress));
                }

                if (Task.WaitAll(connectTasks.ToArray(), TimeSpan.FromSeconds(ClientCount)))
                {
                    throw new TimeoutException($"Waited {ClientCount} seconds to connect {ClientCount} clients to {obj0.BoundAddress}, but the operation timed out.");
                }

                foreach (var task in connectTasks)
                {
                    // storing our local address for comparison purposes
                    obj0 = obj0.AddLocalChannel(task.Result).AddClient((IPEndPoint) task.Result.LocalAddress);
                }
                return obj0;
            }

            public override bool Pre(ITcpServerSocketModel _arg1)
            {
                // need at least 1 client
                return base.Pre(_arg1) && ClientCount > 0;
            }

            public override string ToString()
            {
                return $"ClientConnect(Count={ClientCount})";
            }
        }

        public class ClientDisconnect : NonBindCommand
        {
            public static Gen<Operation<ITcpServerSocketModel, ITcpServerSocketModel>> Generator()
            {
                return Gen.Choose(1,10).Select(x => (Operation<ITcpServerSocketModel, ITcpServerSocketModel>)new ClientDisconnect(x));
            }

            public ClientDisconnect(int clientCount)
            {
                ClientCount = clientCount;
            }

            public int ClientCount { get; private set; }

            public override Property Check(ITcpServerSocketModel obj0, ITcpServerSocketModel obj1)
            {
                return (TcpServerSocketModelComparer.Instance.Equals(obj0, obj1))
                    .Label(
                        $"Expected {obj1.RemoteClients.Count} total connections, but found only {obj0.RemoteClients.Count}");
            }

            public override ITcpServerSocketModel Run(ITcpServerSocketModel obj0)
            {
                var clientsToBeDisconnected = obj0.LocalChannels.Take(ClientCount).ToList();
                var disconnectTasks = new List<Task>();
                foreach (var client in clientsToBeDisconnected)
                {
                    disconnectTasks.Add(client.DisconnectAsync());
                }

                if (Task.WaitAll(disconnectTasks.ToArray(), TimeSpan.FromSeconds(ClientCount)))
                {
                    throw new TimeoutException($"Waited {ClientCount} seconds to disconnect {ClientCount} clients from {obj0.BoundAddress}, but the operation timed out.");
                }

                foreach (var client in clientsToBeDisconnected)
                {
                    obj0 = obj0.RemoveClient((IPEndPoint) client.LocalAddress).RemoveLocalChannel(client);
                }
                return obj0;
            }

            public override bool Pre(ITcpServerSocketModel _arg1)
            {
                return base.Pre(_arg1) && ClientCount <= _arg1.LocalChannels.Count;
            }

            public override string ToString()
            {
                return $"ClientDisconnect(Count={ClientCount})";
            }
        }

        public class ClientWrite : NonBindCommand
        {
            public static Gen<Operation<ITcpServerSocketModel, ITcpServerSocketModel>> Generator()
            {
                return Gen.ArrayOf(Arb.Default.Int32().Generator).Select(x => (Operation<ITcpServerSocketModel, ITcpServerSocketModel>)new ClientWrite(x));
            }

            private readonly int[] _writes;

            public ClientWrite(int[] writes)
            {
                _writes = writes;
            }

            public override Property Check(ITcpServerSocketModel obj0, ITcpServerSocketModel obj1)
            {
                // need to give the inbound side a chance to catch up if the load was large.
                Task.Delay(100).Wait();

                var result = (TcpServerSocketModelComparer.Instance.Equals(obj0, obj1))
                    .Label(
                        $"Expected [{string.Join(",", obj1.LastReceivedMessages.OrderBy(x => x))}] received by clients, but found only [{string.Join(",", obj0.LastReceivedMessages.OrderBy(x => x))}]");

                obj0 = obj0.ClearMessages();
                obj1 = obj1.ClearMessages();

                return result;
            }

            public override ITcpServerSocketModel Run(ITcpServerSocketModel obj0)
            {
                var channels = obj0.LocalChannels;
                var maxIndex = channels.Count-1;

                // write and read order will be different, but our equality checker knows how to deal with that.
                var rValue = obj0.WriteMessages(_writes).ReceiveMessages(_writes);

                var tasks = new ConcurrentBag<Task>();
                // generate a distribution of writes concurrently across all channels
                var loopResult = Parallel.ForEach(_writes, i =>
                {
                    var nextChannel = channels[ThreadLocalRandom.Current.Next(0, maxIndex)];

                    // do write and flush for all writes
                    tasks.Add(nextChannel.WriteAndFlushAsync(i)); 
                });

                // wait for tasks to finish queuing
                SpinWait.SpinUntil(() => loopResult.IsCompleted, TimeSpan.FromMilliseconds(100));

                // picking big timeouts here just in case the input list is large
                var timeout = TimeSpan.FromSeconds(5);
                if (!Task.WaitAll(tasks.ToArray(), timeout))
                {
                    throw new TimeoutException($"Expected to be able to complete {_writes.Length} operations in under {timeout.TotalSeconds} seconds, but the operation timed out.");
                }

                return rValue;
            }

            public override bool Pre(ITcpServerSocketModel _arg1)
            {
                return base.Pre(_arg1)
                       && _arg1.LocalChannels.Count > 0  // need at least 1 local channel
                       && _writes.Length > 0; // need at least 1 write
            }
        }

        #endregion
    }
}

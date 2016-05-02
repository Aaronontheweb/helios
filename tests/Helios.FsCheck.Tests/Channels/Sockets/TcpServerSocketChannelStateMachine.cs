using FsCheck;
using FsCheck.Experimental;
using Helios.FsCheck.Tests.Channels.Sockets.Models;
using static Helios.FsCheck.Tests.HeliosModelHelpers;

namespace Helios.FsCheck.Tests.Channels.Sockets
{
    public class TcpServerSocketChannelStateMachine : Machine<ITcpServerSocketModel, ITcpServerSocketModel>
    {
        public override Gen<Operation<ITcpServerSocketModel, ITcpServerSocketModel>> Next(ITcpServerSocketModel obj0)
        {
            throw new System.NotImplementedException();
        }

        public override Arbitrary<Setup<ITcpServerSocketModel, ITcpServerSocketModel>> Setup { get; }

        #region Setup



        #endregion
    }
}

using FsCheck;
using FsCheck.Experimental;
using FsCheck.Xunit;
using Xunit;

namespace Helios.FsCheck.Tests.Channels.Sockets
{
    public class TcpSocketServerSpecs
    {
        [Fact]
        public void TcpSeverSocketChannel_should_obey_model()
        {
            var model = new TcpServerSocketChannelStateMachine();
            model.ToProperty().Check(new Configuration() { MaxNbOfTest = 1000});
        }
    }
}

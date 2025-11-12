using Tower.Net.Protocol;
using Tower.Net.Session;
using Tower.Net.Transport;
using FluentAssertions;
using Xunit;

public class NetLoopbackTests
{
    [Fact]
    public void Join_And_Rpc_Echo()
    {
        var serverTx = new LoopbackTransport();
        var clientTx = serverTx; // loopback shares queue for simplicity
        var server = new NetServer(serverTx);
        var client = new NetClient(clientTx);
        server.Start();
        client.Connect("tester");
        server.Poll();
        client.Poll();
        client.ClientId.Should().BeGreaterThan(0);
        client.SendRpc("ping", "hello");
        server.Poll();
        client.Poll();
    }
}

using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class AuthHandshakeTests
{
 [Fact]
 public void Auth_Happy_Path_Completes_And_Joins()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.Start(); client.Connect("p1");
 for (int i=0;i<50 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 client.ClientId.Should().BeGreaterThan(0);
 }

 [Fact]
 public void Auth_Fails_With_Wrong_Key()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 client.SetAuthKey(System.Text.Encoding.UTF8.GetBytes("wrong-key"));
 server.Start(); client.Connect("p1");
 for (int i=0;i<50;i++) { server.Poll(); client.Poll(); }
 client.ClientId.Should().Be(0);
 }
}

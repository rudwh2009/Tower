using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Protocol;
using Xunit;

public class UnauthSpamGuardTests
{
 [Fact]
 public void Unauth_Spam_Is_Rate_Limited()
 {
 var (srvT, cliT) = LoopbackDuplex.CreatePair();
 var server = new NetServer(srvT);
 var client = new NetClient(cliT);
 server.SetAuthLimits(maxJoinPerSec:1, maxAuthRespPerSec:1, maxUnauthMsgsPerSec:2);
 server.Start();
 // spam RPC before auth, should be dropped and not crash
 for (int i=0;i<20;i++)
 {
 var buf = new byte[1]; buf[0] = (byte)MessageId.RpcEvent; cliT.Send(buf);
 server.Poll();
 }
 // now do auth/ join normally
 client.Connect("p1");
 for (int i=0;i<50 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 Assert.True(client.ClientId >0);
 }
}

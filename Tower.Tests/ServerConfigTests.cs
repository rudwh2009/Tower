using FluentAssertions;
using Tower.Net.Session;
using Tower.Net.Transport;
using Xunit;

public class ServerConfigTests
{
 [Fact]
 public void Applies_Config_To_Server()
 {
 var t = new LoopbackTransport();
 var server = new NetServer(t);
 var cfg = new NetServerConfig
 {
 SharedKey = "k1",
 OutboundBytesPerSecond =10_000,
 InboundCapBytes =128*1024,
 HeartbeatTimeoutMs =7000,
 AuthLimits = (1,2,3),
 MaxRpcBytes =2048,
 InputRateCapPerSec =30,
 RpcRateCapPerSec =20,
 ModBytesPerSecond =256*1024,
 ModMaxChunkBytes =16*1024,
 InterestRadius =256f,
 };
 cfg.ApplyTo(server);
 // smoke: start and stop
 server.Start(); server.Stop();
 }
}

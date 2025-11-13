using FluentAssertions;
using Tower.Net.Session;
using Xunit;

public class DeltaToggleConfigTests
{
 [Fact]
 public void UseDeltas_Config_Applies()
 {
 var s = new NetServerConfig { UseDeltas = true };
 var server = new NetServer(new Tower.Net.Transport.LoopbackTransport());
 s.ApplyTo(server);
 // Can't observe directly without deeper hooks; smoke test just ensures no exception.
 server.Start(); server.Stop();
 }
}

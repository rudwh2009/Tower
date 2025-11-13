using Serilog;
using Tower.Net.Abstractions;
using Tower.Net.Protocol;
using Tower.Net.Protocol.Messages;

namespace Tower.Net.Session;

public sealed class ServerSession
{
 public int ClientId { get; }
 private readonly ITransport _transport;
 private long _lastSeenMs;
 private readonly int _timeoutMs;
 public bool Connected { get; private set; }
 public ServerSession(int clientId, ITransport transport, int timeoutMs =5000)
 {
 ClientId = clientId; _transport = transport; _timeoutMs = timeoutMs; Connected = true; _lastSeenMs = Environment.TickCount64;
 }
 public void MarkSeen() => _lastSeenMs = Environment.TickCount64;
 public bool IsTimedOut(long nowMs) => (nowMs - _lastSeenMs) > _timeoutMs;
 public void Send(ReadOnlySpan<byte> payload)
 {
 _transport.Send(payload.ToArray());
 }
 public void SendHeartbeat()
 {
 var buf = new byte[1]; buf[0] = (byte)MessageId.Heartbeat; _transport.Send(buf);
 }
}

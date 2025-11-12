using Tower.Net.Abstractions;
using Tower.Net.Protocol;
using Tower.Net.Protocol.Messages;
using Serilog;

namespace Tower.Net.Session;

public sealed class NetClient
{
 private readonly ITransport _transport;
 public int ClientId { get; private set; }
 public NetClient(ITransport transport) => _transport = transport;
 public void Connect(string name)
 {
 _transport.Connect();
 var req = new JoinRequest(name).Serialize();
 var buf = new byte[1 + req.Length];
 buf[0] = (byte)MessageId.JoinRequest;
 req.CopyTo(buf.AsSpan(1));
 _transport.Send(buf);
 }
 public void Poll()
 {
 _transport.Poll(data =>
 {
 var id = (MessageId)data.Span[0];
 var payload = data.Slice(1);
 if (id == MessageId.JoinAccepted)
 {
 var acc = JoinAccepted.Deserialize(payload.Span);
 ClientId = acc.ClientId;
 Log.Information("JoinAccepted: {ClientId}", ClientId);
 }
 else if (id == MessageId.RpcEvent)
 {
 var ev = RpcEvent.Deserialize(payload.Span);
 Log.Information("RpcEvent: {Event} {Payload}", ev.Event, ev.Payload);
 }
 });
 }
 public void SendRpc(string @event, string payload)
 {
 var rpc = new RpcEvent(@event, payload).Serialize();
 var buf = new byte[1 + rpc.Length];
 buf[0] = (byte)MessageId.RpcEvent;
 rpc.CopyTo(buf.AsSpan(1));
 _transport.Send(buf);
 }
}

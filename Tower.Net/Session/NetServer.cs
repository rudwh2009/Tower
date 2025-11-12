using Tower.Net.Abstractions;
using Tower.Net.Protocol;
using Tower.Net.Protocol.Messages;
using Serilog;

namespace Tower.Net.Session;

public sealed class NetServer
{
 private readonly ITransport _transport;
 private int _nextClientId =1;
 public NetServer(ITransport transport) => _transport = transport;
 public void Start()
 {
 _transport.Bind();
 Log.Information("NetServer started");
 }
 public void Poll()
 {
 _transport.Poll(data =>
 {
 if (data.Length ==0) return;
 var msgId = (MessageId)data.Span[0];
 var payload = data.Slice(1);
 switch (msgId)
 {
 case MessageId.JoinRequest:
 var req = JoinRequest.Deserialize(payload.Span);
 Log.Information("JoinRequest from {Name}", req.Name);
 var accepted = new JoinAccepted(_nextClientId++).Serialize();
 var outBuf = new byte[1 + accepted.Length];
 outBuf[0] = (byte)MessageId.JoinAccepted;
 accepted.CopyTo(outBuf.AsSpan(1));
 _transport.Send(outBuf);
 break;
 case MessageId.RpcEvent:
 var ev = RpcEvent.Deserialize(payload.Span);
 Log.Information("RpcEvent {Event} {Payload}", ev.Event, ev.Payload);
 // echo back
 _transport.Send(data.ToArray());
 break;
 }
 });
 }
}

#if LITENETLIB
using System.Collections.Concurrent;
using LiteNetLib;
using LiteNetLib.Utils;
using Tower.Net.Abstractions;

namespace Tower.Net.Transport;

public sealed class LiteNetLibClientTransport : INetEventListener, ITransport
{
 private NetManager? _net;
 private NetPeer? _peer;
 private readonly string _addr;
 private readonly int _port;
 private readonly ConcurrentQueue<byte[]> _incoming = new();
 public LiteNetLibClientTransport(string addr ="127.0.0.1", int port =9050) { _addr = addr; _port = port; }
 public void Bind() { }
 public void Connect()
 {
 if (_net is not null) return;
 _net = new NetManager(this) { UnsyncedEvents = true, UpdateTime =15 };
 _net.Start();
 _peer = _net.Connect(_addr, _port, "tower");
 }
 public void Send(ReadOnlyMemory<byte> data) { _peer?.Send(data.Span, DeliveryMethod.ReliableOrdered); }
 public void Poll(Action<ReadOnlyMemory<byte>> onMessage)
 {
 _net?.PollEvents();
 while (_incoming.TryDequeue(out var buf)) onMessage(buf);
 }
 public void Disconnect() { _net?.Stop(); _net = null; _peer = null; }
 // INetEventListener
 public void OnConnectionRequest(ConnectionRequest request) { request.Reject(); }
 public void OnPeerConnected(NetPeer peer) { }
 public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { }
 public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode) { }
 public void OnNetworkReceive(NetPeer peer, NetDataReader reader, DeliveryMethod deliveryMethod) { _incoming.Enqueue(reader.GetRemainingBytes()); }
 public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType) { }
 public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
}
#endif

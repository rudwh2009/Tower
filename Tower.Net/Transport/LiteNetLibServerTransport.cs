#if LITENETLIB
using System.Collections.Concurrent;
using LiteNetLib;
using LiteNetLib.Utils;
using Tower.Net.Abstractions;

namespace Tower.Net.Transport;

public sealed class LiteNetLibServerTransport : INetEventListener, IServerTransport
{
 private NetManager? _net;
 private readonly int _port;
 private readonly ConcurrentDictionary<NetPeer, ConnectionId> _map = new();
 private readonly ConcurrentQueue<(ConnectionId id, byte[] buf)> _incoming = new();
 private int _nextId =1;

 public LiteNetLibServerTransport(int port =9050) { _port = port; }
 public IEnumerable<ConnectionId> Connections => _map.Values;
 public void Start()
 {
 _net = new NetManager(this) { UnsyncedEvents = true, UpdateTime =15 };
 _net.Start(_port);
 }
 public void Stop() { _net?.Stop(); _net = null; _map.Clear(); }
 public void Send(ConnectionId to, ReadOnlyMemory<byte> data)
 {
 if (_net is null) return;
 foreach (var kv in _map) if (kv.Value.Equals(to)) { kv.Key.Send(data.Span, DeliveryMethod.ReliableOrdered); break; }
 }
 public void Broadcast(ReadOnlyMemory<byte> data)
 {
 if (_net is null) return;
 foreach (var p in _net.ConnectedPeerList) p.Send(data.Span, DeliveryMethod.ReliableOrdered);
 }
 public void Poll(Action<ConnectionId, ReadOnlyMemory<byte>> onMessage)
 {
 _net?.PollEvents();
 while (_incoming.TryDequeue(out var tup)) onMessage(tup.id, tup.buf);
 }

 // INetEventListener
 public void OnConnectionRequest(ConnectionRequest request)
 {
 request.AcceptIfKey("tower");
 }
 public void OnPeerConnected(NetPeer peer)
 {
 var id = new ConnectionId(Interlocked.Increment(ref _nextId));
 _map[peer] = id;
 }
 public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
 {
 _map.TryRemove(peer, out _);
 }
 public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode) { }
 public void OnNetworkReceive(NetPeer peer, NetDataReader reader, DeliveryMethod deliveryMethod)
 {
 var buf = reader.GetRemainingBytes();
 if (_map.TryGetValue(peer, out var id)) _incoming.Enqueue((id, buf));
 }
 public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType) { }
 public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
}
#endif

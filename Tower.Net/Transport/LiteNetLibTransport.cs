#if LITENETLIB
using System.Collections.Concurrent;
using LiteNetLib;
using LiteNetLib.Utils;
using Tower.Net.Abstractions;

namespace Tower.Net.Transport;

/// <summary>
/// LiteNetLib-based transport adapter. Note: with the current ITransport shape,
/// server Send() will broadcast to all connected peers (no per-peer routing).
/// Use loopback transports for unit tests; this adapter is intended for manual runs.
/// </summary>
public sealed class LiteNetLibTransport : INetEventListener, ITransport
{
 private readonly bool _isServer;
 private readonly string _address;
 private readonly int _port;
 private NetManager? _net;
 private NetPeer? _clientPeer; // for client side
 private readonly ConcurrentQueue<byte[]> _incoming = new();
 private readonly object _gate = new();

 public LiteNetLibTransport(bool isServer, int port =9050, string address = "127.0.0.1")
 {
 _isServer = isServer; _port = port; _address = address;
 }

 public void Bind()
 {
 if (!_isServer) return;
 lock (_gate)
 {
 if (_net is not null) return;
 _net = new NetManager(this) { UnsyncedEvents = true, UpdateTime =15 };
 _net.Start(_port);
 }
 }

 public void Connect()
 {
 if (_isServer) return;
 lock (_gate)
 {
 if (_net is not null) return;
 _net = new NetManager(this) { UnsyncedEvents = true, UpdateTime =15 };
 _net.Start();
 _clientPeer = _net.Connect(_address, _port, "tower");
 }
 }

 public void Send(ReadOnlyMemory<byte> data)
 {
 lock (_gate)
 {
 if (_net is null) return;
 if (_isServer)
 {
 foreach (var p in _net.ConnectedPeerList)
 {
 p.Send(data.Span, DeliveryMethod.ReliableOrdered);
 }
 }
 else
 {
 _clientPeer?.Send(data.Span, DeliveryMethod.ReliableOrdered);
 }
 }
 }

 public void Poll(Action<ReadOnlyMemory<byte>> onMessage)
 {
 // pump events
 lock (_gate) { _net?.PollEvents(); }
 while (_incoming.TryDequeue(out var msg)) onMessage(msg);
 }

 public void Disconnect()
 {
 lock (_gate)
 {
 _net?.Stop();
 _net = null; _clientPeer = null;
 }
 }

 // INetEventListener
 public void OnConnectionRequest(ConnectionRequest request)
 {
 if (_isServer && _net is not null) request.AcceptIfKey("tower"); else request.Reject();
 }
 public void OnPeerConnected(NetPeer peer) { }
 public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { }
 public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode) { }
 public void OnNetworkReceive(NetPeer peer, NetDataReader reader, DeliveryMethod deliveryMethod)
 {
 var bytes = reader.GetRemainingBytes();
 _incoming.Enqueue(bytes);
 }
 public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType) { }
 public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
}
#endif

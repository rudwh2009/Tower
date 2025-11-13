using System.Collections.Concurrent;
using Tower.Net.Abstractions;

namespace Tower.Net.Transport;

public static class LoopbackDuplex
{
 public sealed record ChaosProfile(double Loss =0.0, int JitterMs =0, double Duplicate =0.0, double Reorder =0.0);
 private sealed class Endpoint : ITransport
 {
 private readonly ConcurrentQueue<byte[]> _in;
 private readonly ConcurrentQueue<byte[]> _out;
 private readonly ChaosProfile _chaos;
 private readonly Random _rng = new(1234);
 public Endpoint(ConcurrentQueue<byte[]> incoming, ConcurrentQueue<byte[]> outgoing, ChaosProfile chaos)
 { _in = incoming; _out = outgoing; _chaos = chaos; }
 public void Bind() { }
 public void Connect() { }
 public void Send(ReadOnlyMemory<byte> data)
 {
 if (_chaos.Loss >0 && _rng.NextDouble() < _chaos.Loss) return;
 var bytes = data.ToArray();
 void Enq(byte[] b)
 {
 if (_chaos.JitterMs >0)
 {
 var delay = (int)Math.Max(0, _chaos.JitterMs + (_rng.Next(-_chaos.JitterMs, _chaos.JitterMs)));
 _ = Task.Run(async () => { await Task.Delay(delay).ConfigureAwait(false); _out.Enqueue(b); });
 }
 else
 {
 _out.Enqueue(b);
 }
 }
 // duplicate
 if (_chaos.Duplicate >0 && _rng.NextDouble() < _chaos.Duplicate) Enq(bytes);
 // reorder: enqueue a marker to simulate OOO occasionally
 if (_chaos.Reorder >0 && _rng.NextDouble() < _chaos.Reorder) Enq(Array.Empty<byte>());
 Enq(bytes);
 }
 public void Poll(Action<ReadOnlyMemory<byte>> onMessage)
 {
 while (_in.TryDequeue(out var msg))
 {
 if (msg.Length ==0) continue; // drop chaos marker
 onMessage(msg);
 }
 }
 public void Disconnect() { }
 }

 public static (ITransport server, ITransport client) CreatePair()
 {
 var a = new ConcurrentQueue<byte[]>();
 var b = new ConcurrentQueue<byte[]>();
 return (new Endpoint(a,b, new ChaosProfile()), new Endpoint(b,a, new ChaosProfile()));
 }
 public static (ITransport server, ITransport client) CreatePair(ChaosProfile chaos)
 {
 var a = new ConcurrentQueue<byte[]>();
 var b = new ConcurrentQueue<byte[]>();
 return (new Endpoint(a,b, chaos), new Endpoint(b,a, chaos));
 }
}

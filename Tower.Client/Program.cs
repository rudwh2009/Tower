using Serilog;
using Tower.Net;
using ServerEntry = Tower.Server.Program;
using Tower.Net.Transport;
using Tower.Server;

namespace Tower.Client;

public static class Program
{
 public static int Main(string[] args)
 {
 Serilog.Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
 var mode = RunArgs.Parse(args);
 switch (mode)
 {
 case RunMode.Dedicated:
 {
 Serilog.Log.Information("Starting dedicated server");
 return ServerEntry.RunDedicatedServer();
 }
 case RunMode.ListenCoop:
 case RunMode.ListenSolo:
 {
 Serilog.Log.Information("Starting listen server (in-process) and client");
 var (srvT, cliT) = LoopbackDuplex.CreatePair();
 var serverThread = new Thread(() =>
 {
 try
 {
 var server = new GameServer();
 server.SetTransport(srvT);
 server.Run();
 }
 catch (Exception ex)
 {
 Serilog.Log.Error(ex, "Server thread crashed");
 }
 });
 serverThread.IsBackground = true;
 serverThread.Start();
 var smoke = args.Contains("--test-smoke");
 using (var game = new GameClient(smoke))
 {
 var content = Path.Combine(AppContext.BaseDirectory, "Content");
 var sync = new ListenSync(content);
 var cache = new CacheIndex(content);
 var rev = new RevisionCache(content);
 var client = new Tower.Net.Session.NetClient(cliT, sync, cache, rev);
 client.Connect("player1");
 game.AttachListenClient(client, sync);
 game.Run();
 }
 return 0;
 }
 default:
 {
 var smoke = args.Contains("--test-smoke");
 using (var game = new GameClient(smoke))
 {
 game.Run();
 }
 return 0;
 }
 }
 }
}

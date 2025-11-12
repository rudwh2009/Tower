using Serilog;
using Tower.Net;
using ServerEntry = Tower.Server.Program;

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
 Serilog.Log.Information("Starting dedicated server");
 return ServerEntry.RunDedicatedServer();
 case RunMode.ListenCoop:
 Serilog.Log.Information("Starting listen server (coop)");
 return ServerEntry.RunListenServer(true);
 case RunMode.ListenSolo:
 default:
 var smoke = args.Contains("--test-smoke");
 using (var game = new GameClient(smoke))
 {
 game.Run();
 }
 return 0;
 }
 }
}

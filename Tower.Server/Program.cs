using Serilog;
using Tower.Net;

namespace Tower.Server;

public static class Program
{
 public static int RunListenServer(bool coop)
 {
 Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
 var server = new GameServer();
 server.Run();
 return 0;
 }

 public static int RunDedicatedServer()
 {
 Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
 var server = new GameServer();
 server.Run();
 return 0;
 }

 public static void Main(string[] args)
 {
 Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
 var opts = ServerOptions.Parse(args);
 if (!string.IsNullOrEmpty(opts.ConfigPath)) Environment.SetEnvironmentVariable("TOWER_NETCFG", opts.ConfigPath);
 var server = new GameServer();
 server.Run();
 }
}

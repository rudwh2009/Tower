using Serilog;

namespace Tower.Server;

public static class Program
{
 public static void Main(string[] args)
 {
 Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
 var server = new GameServer();
 server.Run();
 }
}

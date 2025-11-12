using Serilog;

namespace Tower.Client;

public static class Program
{
 public static void Main(string[] args)
 {
 Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
 var smoke = args.Contains("--test-smoke");
 using var game = new GameClient(smoke);
 game.Run();
 }
}

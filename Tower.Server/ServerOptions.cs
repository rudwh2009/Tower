namespace Tower.Server;

public sealed class ServerOptions
{
 public string? ConfigPath { get; set; }
 public bool Dedicated { get; set; }

 public static ServerOptions Parse(string[] args)
 {
 var opts = new ServerOptions();
 for (int i=0;i<args.Length;i++)
 {
 var a = args[i];
 switch (a)
 {
 case "--config":
 case "-c":
 if (i+1 < args.Length) { opts.ConfigPath = args[++i]; }
 break;
 case "--dedicated":
 opts.Dedicated = true;
 break;
 }
 }
 return opts;
 }
}

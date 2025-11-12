namespace Tower.Net;

public enum RunMode
{
 ListenSolo,
 ListenCoop,
 Dedicated,
}

public static class RunArgs
{
 public static RunMode Parse(string[] args)
 {
 foreach (var a in args)
 {
 var v = a.Trim().ToLowerInvariant();
 if (v == "--mode=solo" || v == "--solo" || v == "-solo") return RunMode.ListenSolo;
 if (v == "--mode=coop" || v == "--coop" || v == "-coop") return RunMode.ListenCoop;
 if (v == "--mode=dedicated" || v == "--dedicated" || v == "-dedicated") return RunMode.Dedicated;
 }
 return RunMode.ListenSolo;
 }
}

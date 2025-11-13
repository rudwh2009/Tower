namespace Tower.Net.Session;

public interface IModPackCache
{
 bool Has(string id, string sha256);
}

using System.Web;

namespace MonadDashboard.Providers;

public class HyperSyncProvider(string baseUrl)
{
    private readonly Dictionary<string, string> _parameters = new();
    
    public HyperSyncProvider WithStartBlock(long startBlock)
    {
        _parameters["start"] = startBlock.ToString().ToLowerInvariant();
        return this;
    }
    
    public HyperSyncProvider WithEndBlock(long endBlock)
    {
        _parameters["end"] = endBlock.ToString().ToLowerInvariant();
        return this;
    }
    
    public Uri Build()
    {
        var ub = new UriBuilder(baseUrl);
        var query = HttpUtility.ParseQueryString(string.Empty);

        foreach (var kv in _parameters)
        {
            query[kv.Key] = kv.Value;
        }
        
        ub.Query = query.ToString();

        _parameters.Clear();
        
        return ub.Uri;
    }
}
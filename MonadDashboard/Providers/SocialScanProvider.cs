using System.Web;
using MonadDashboard.Models.SocialScan;
using Action = MonadDashboard.Models.SocialScan.Action;

namespace MonadDashboard.Providers;

public class SocialScanProvider
{
    private readonly string _baseUrl;
    private readonly Dictionary<string, string> _parameters = new();
    private readonly HashSet<string> _reservedKeys = new(StringComparer.OrdinalIgnoreCase);
    private void ClearParameters()
    {
        var keysToRemove = _parameters.Keys
            .Where(k => !_reservedKeys.Contains(k))
            .ToList();
        
        foreach (var key in keysToRemove)
            _parameters.Remove(key);
    }
    
    public SocialScanProvider(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl;
        
        _reservedKeys.Add("apikey");
        _reservedKeys.Add("sort");
        
        _parameters["apikey"] = apiKey;
        _parameters["sort"] = "asc";
    }

    public SocialScanProvider WithModule(Module module)
    {
        _parameters["module"] = module.ToString().ToLowerInvariant();
        return this;
    }

    public SocialScanProvider WithAction(Action action)
    {
        _parameters["action"] = action.ToString().ToLowerInvariant();
        return this;
    }

    public SocialScanProvider WithContractAddress(string address)
    {
        _parameters["contractaddresses"] = address.ToLowerInvariant();
        return this;
    }
    
    public SocialScanProvider WithStartDate(DateTime startDate)
    { 
        _parameters["startdate"] = startDate.ToString("yyyy-MM-dd");
        return this;
    }

    public SocialScanProvider WithEndDate(DateTime endDate)
    { 
        _parameters["enddate"] = endDate.ToString("yyyy-MM-dd");
        return this;
    }

    public Uri Build()
    {
        var ub = new UriBuilder(_baseUrl);
        var query = HttpUtility.ParseQueryString(string.Empty);

        foreach (var kv in _parameters)
        {
            query[kv.Key] = kv.Value;
        }
        
        ub.Query = query.ToString();

        ClearParameters();
        
        return ub.Uri;
    }
}
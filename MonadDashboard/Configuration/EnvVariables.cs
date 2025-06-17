namespace MonadDashboard.Configuration;

public class EnvVariables
{
    public string HyperSyncRpc { get; set; } = string.Empty;
    public string SocialScanPublicApiKey { get; set; } = string.Empty;
    public string SocialScanApi { get; set; } = string.Empty;
    public string HyperSyncApi { get; set; } = string.Empty;
    public int BatchSize { get; set; }
}
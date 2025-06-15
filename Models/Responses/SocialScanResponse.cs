namespace MonadDashboard.Models.Responses;

public class SocialScanResponse<T>
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public T Result { get; set; } = default(T)!;
}
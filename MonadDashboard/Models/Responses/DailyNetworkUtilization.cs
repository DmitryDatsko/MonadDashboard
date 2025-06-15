namespace MonadDashboard.Models.Responses;

public record DailyNetworkUtilization(
    DateTime? UtcDate = null,
    long? UnixTimeStamp = null,
    decimal? NetworkUtilization = null);
using System.Numerics;

namespace MonadDashboard.Models.Responses;

public record DailyTransactionCount(
    DateTime? UtcDate = null,
    long? UnixTimeStamp = null,
    long? TransactionCount = null);
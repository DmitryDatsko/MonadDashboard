using System.Numerics;

namespace MonadDashboard.Models.Responses;

public record DailyTransactionFee(
        DateTime? UtcDate = null,
        long? UnixTimeStamp = null,
        string? TransactionFee = null);
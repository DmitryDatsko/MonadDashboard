namespace MonadDashboard.Models.Responses;

public record Block(
    long Height,
    string Hash,
    long TxAmount,
    string GasUsed,
    string GasLimit,
    DateTime Timestamp);
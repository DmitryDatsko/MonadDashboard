using Org.BouncyCastle.Math;

namespace MonadDashboard.Models.Responses;

public record Transaction(
    string TxHash,
    string From,
    string To,
    string ValueWei,
    DateTime Timestamp);
namespace MonadDashboard.Models.Responses;

public record HypersyncTransaction(
    long Start,
    long End,
    long Transactions);
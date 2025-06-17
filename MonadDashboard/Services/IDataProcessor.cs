using System.Numerics;
using MonadDashboard.Models;

namespace MonadDashboard.Services;

public interface IDataProcessor
{
    long LatestBlock { get; }
    decimal AvgTps { get; }
    decimal AvgBlockTime { get; }
    BigInteger AvgFeeWei { get; }
    BigInteger AvgGasWei { get; }
    TotalTransaction TotalTransaction { get; }

    Task UpdateLatestBlockAsync();
    Task UpdateAvgTpsAsync();
    Task UpdateAvgBlockTimeAsync();
    Task UpdateAvgFeeWeiAsync();
    Task UpdateAvgGasWeiAsync();
    Task UpdateTotalTransactionAsync();
    Task UpdateRemainingTransactionsAsync();
    
    Task UpdateDataAsync();
    dynamic GetMetrics();
}
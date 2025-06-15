using System.Numerics;
using MonadDashboard.Models;

namespace MonadDashboard.Services;

public interface IDataProcessor
{
    BigInteger LatestBlock { get; }
    decimal AvgTps { get; }
    decimal AvgBlockTime { get; }
    BigInteger AvgFeeWei { get; }
    BigInteger AvgGasWei { get; }
    TotalTransaction TotalTransaction { get; }

    Task UpdateLatestBlockAsync();
    Task UpdateAvgTpsAsync();
    Task UpdateAvgBlockTime();
    Task UpdateAvgFeeWei();
    Task UpdateAvgGasWei();
    Task UpdateTotalTransaction();
    
    Task UpdateDataAsync();
    dynamic GetMetrics();
}
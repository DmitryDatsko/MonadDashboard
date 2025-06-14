using System.Numerics;

namespace MonadDashboard.Services;

public interface IDataProcessor
{
    BigInteger LatestBlock { get; }
    decimal AvgTps { get; }
    decimal AvgBlockTime { get; }
    BigInteger AvgFeeWei { get; }
    BigInteger AvgGasWei { get; }

    Task UpdateLatestBlockAsync();
    Task UpdateAvgTpsAsync();
    Task UpdateAvgBlockTime();
    Task UpdateAvgFeeWei();
    Task UpdateAvgGasWei();
    
    Task UpdateDataAsync();
    dynamic GetMetrics();
}
using System.Dynamic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using MonadDashboard.Extensions;
using Nethereum.Util;
using Nethereum.Web3;
using Org.BouncyCastle.Tls.Crypto;

namespace MonadDashboard.Services;

public class DataProcessor : IDataProcessor
{
    private readonly IRequests _requests;
    private readonly ILogger<DataProcessor> _logger;
    public BigInteger LatestBlock { get; private set; }
    public decimal AvgTps { get; private set; }
    public decimal AvgBlockTime { get; private set; }
    public BigInteger AvgFeeWei { get; private set; }
    public BigInteger AvgGasWei { get; private set; }

    public DataProcessor(IRequests requests,
        ILogger<DataProcessor> logger)
    {
        _requests = requests;
        _logger = logger;
    }
    
    public async Task UpdateLatestBlockAsync()
    {
        LatestBlock = await _requests.GetCurrentBlockAsync();
    }

    public async Task UpdateAvgTpsAsync()
    {
        var blocks = await _requests.GetLastBlockWithTransaction(LatestBlock);
        
        if(blocks.Count < 2)
            return;
        
        var totalTx = blocks.Select(tx => tx.Transactions.Length)
            .Sum();
        var totalSec = (decimal)(blocks[^1].Timestamp.Value - blocks[0].Timestamp.Value);
        
        AvgTps = Math.Round(totalTx / totalSec, 2);
    }

    public async Task UpdateAvgBlockTime()
    {
        var blocks = await _requests.GetLastBlockWithTransaction(LatestBlock);

        var times = blocks
            .Select(b => DateTimeOffset
                .FromUnixTimeSeconds((long)b.Timestamp.Value)).ToList();

        if (times.Count < 2)
        {
            AvgBlockTime = 0m;
            _logger.LogWarning("Not enough blocks ({Count}) to compute average block time, defaulting to 0", times.Count);
            return;
        }
        
        var spans = times
            .Zip(times.Skip(1), (prev, next) => next - prev);
        
        double avgTicks = spans.Average(span => span.Ticks);
        var avgSpan = TimeSpan.FromTicks((long)avgTicks);
        
        AvgBlockTime = (decimal)avgSpan.TotalSeconds;
    }

    public async Task UpdateAvgFeeWei()
    {
        var blocks = await _requests.GetLastBlockWithTransaction(LatestBlock);

        var fees = blocks
            .SelectMany(tx => tx.Transactions)
            .Select(tx => tx.GasPrice.Value * tx.Gas.Value)
            .ToList();
        
        if(!fees.Any())
            return;
        
        AvgFeeWei = fees.Sum() / fees.Count;
    }

    public async Task UpdateAvgGasWei()
    {
        var blocks = await _requests.GetLastBlockWithTransaction(LatestBlock);

        var gases = blocks
            .SelectMany(tx => tx.Transactions)
            .Select(tx => tx.GasPrice.Value)
            .ToList();
        
        if(!gases.Any())
            return;
        
        AvgGasWei = gases.Sum() / gases.Count;
    }

    public async Task UpdateDataAsync()
    {
        var updateMethods = this.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => 
                m.Name.StartsWith("Update", StringComparison.Ordinal)
                && m.Name != nameof(UpdateDataAsync))
            .OrderBy(m => m.Name != nameof(UpdateLatestBlockAsync))
            .ThenBy(m => m.Name);

        var tasks = updateMethods.Select(async m =>
        {
            try
            {
                var task = (Task)m.Invoke(this, null)!;
                await task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Metric {MethodName} failed", m.Name);
            }
        });

        await Task.WhenAll(tasks);
    }

    public dynamic GetMetrics()
    {
        var expando = new ExpandoObject() as IDictionary<string, object>;

        var props = this.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead);

        foreach (var prop in props)
        {
            var rawValue = prop.GetValue(this);

            expando[prop.Name] = rawValue switch
            {
                decimal dec =>
                    Math.Round(dec, 2, MidpointRounding.AwayFromZero)
                        .ToString("F2", CultureInfo.InvariantCulture),
                
                null => 
                    string.Empty,

                _ => rawValue.ToString() ?? string.Empty
            };
        }
        
        return expando;
    }
}
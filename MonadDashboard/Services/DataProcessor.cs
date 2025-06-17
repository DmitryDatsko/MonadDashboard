using System.Dynamic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using MonadDashboard.Extensions;
using MonadDashboard.Models;

namespace MonadDashboard.Services;

public class DataProcessor : IDataProcessor
{
    private readonly IRequests _requests;
    private readonly ILogger<DataProcessor> _logger;
    private static readonly int ChunkSize = 5000;
    
    private Task? _longRunningTask;
    private readonly object _lock = new();
    
    public long LatestBlock { get; private set; }
    public decimal AvgTps { get; private set; }
    public decimal AvgBlockTime { get; private set; }
    public BigInteger AvgFeeWei { get; private set; }
    public BigInteger AvgGasWei { get; private set; }
    public TotalTransaction TotalTransaction { get; private set; }
    public double SuccessPct { get; private set; }

    public DataProcessor(IRequests requests,
        ILogger<DataProcessor> logger)
    {
        _requests = requests;
        _logger = logger;
        TotalTransaction = new TotalTransaction
        {
            LastestBlock = 0,
            TransactionsAmount = 0
        };
    }
    
    public async Task UpdateLatestBlockAsync()
    {
        LatestBlock = await _requests.GetCurrentBlockAsync();
    }

    public async Task UpdateAvgTpsAsync()
    {
        var blocks = await _requests.GetLastBlockWithTransactionAsync(LatestBlock);
        
        if(blocks.Count < 2)
            return;
        
        var totalTx = blocks.Select(tx => tx.Transactions.Length)
            .Sum();
        var totalSec = (decimal)(blocks[^1].Timestamp.Value - blocks[0].Timestamp.Value);
        
        AvgTps = Math.Round(totalTx / totalSec, 2);
    }

    public async Task UpdateAvgBlockTimeAsync()
    {
        var blocks = await _requests.GetLastBlockWithTransactionAsync(LatestBlock);

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

    public async Task UpdateAvgFeeWeiAsync()
    {
        var blocks = await _requests.GetLastBlockWithTransactionAsync(LatestBlock);

        var fees = blocks
            .SelectMany(tx => tx.Transactions)
            .Select(tx => tx.GasPrice.Value * tx.Gas.Value)
            .ToList();
        
        if(!fees.Any())
            return;
        
        AvgFeeWei = fees.Sum() / fees.Count;
    }

    public async Task UpdateAvgGasWeiAsync()
    {
        var blocks = await _requests.GetLastBlockWithTransactionAsync(LatestBlock);

        var gases = blocks
            .SelectMany(tx => tx.Transactions)
            .Select(tx => tx.GasPrice.Value)
            .ToList();
        
        if(!gases.Any())
            return;
        
        AvgGasWei = gases.Sum() / gases.Count;
    }
    
    public async Task UpdateTotalTransactionAsync()
    {
        if (TotalTransaction.TransactionsAmount == 0)
        {
            var range = await _requests.GetDaysAfterCreatingAsync();
            var txs = await _requests.GetDailyTransactionCountAsync(range);
            
            if (txs == null)
                return;

            TotalTransaction.LastestBlock = await _requests.GetBlockByTimestampAsync(txs[^1].UtcDate);
            TotalTransaction.TransactionsAmount = txs.Select(tx => tx.TransactionCount).Sum();
        }
    }

    public async Task UpdateRemainingTransactionsAsync()
    {
        if (LatestBlock > TotalTransaction.LastestBlock 
            && TotalTransaction.LastestBlock > 0)
        {
            long start = TotalTransaction.LastestBlock + 1;
            long end = LatestBlock;

            try
            {
                for (long chunkStart = start; chunkStart <= end; chunkStart += ChunkSize)
                {
                    long chunkEnd = Math.Min(chunkStart + ChunkSize - 1, end);

                    var resp = await _requests
                        .HypersyncTransactionAsync(chunkStart, chunkEnd);

                    TotalTransaction.LastestBlock = chunkEnd;
                    TotalTransaction.TransactionsAmount += resp.Transactions;
                    
                    _logger.LogInformation($"Transaction added: {resp.Transactions}");
                    
                    await Task.Delay(25);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute remaining transactions");
            }
        }
    }

    public async Task UpdateSuccessPct()
    {
        var receipts = await _requests.GetBlockTransactionsReceiptsAsync(LatestBlock);
        
        int totalTx = receipts.Count;
        int successTx = receipts.Count(r => r.Status.Value == 1);
        double successPct = totalTx == 0
            ? 0
            : successTx * 100.0 / totalTx;
        
        SuccessPct = successPct;
    }

    public async Task UpdateDataAsync()
    {
        lock (_lock)
        {
            if (_longRunningTask == null || _longRunningTask.IsCompleted)
            {
                _longRunningTask = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateRemainingTransactionsAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Long-running update failed");
                    }
                });
            }
        }
        
        var updateMethods = this.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => 
                m.Name.StartsWith("Update", StringComparison.Ordinal)
                && m.Name != nameof(UpdateDataAsync)
                && m.Name != nameof(UpdateRemainingTransactionsAsync))
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
                
                double dou =>
                    Math.Round(dou, 2, MidpointRounding.AwayFromZero)
                        .ToString("F2", CultureInfo.InvariantCulture),
                
                TotalTransaction totalTx =>
                    totalTx.TransactionsAmount.ToString() ?? string.Empty,
                
                null => 
                    string.Empty,

                _ => rawValue.ToString() ?? string.Empty
            };
        }
        
        return expando;
    }
}
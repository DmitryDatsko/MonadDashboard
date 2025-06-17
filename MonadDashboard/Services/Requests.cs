using System.Numerics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MonadDashboard.Configuration;
using MonadDashboard.Models.Responses;
using MonadDashboard.Providers;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Action = MonadDashboard.Models.SocialScan.Action;
using Block = MonadDashboard.Models.Responses.Block;
using Module = MonadDashboard.Models.SocialScan.Module;
using Transaction = MonadDashboard.Models.Responses.Transaction;

namespace MonadDashboard.Services;

public class Requests : IRequests
{
    private readonly Web3 _hypersyncRpc;
    private readonly int _batchSize;
    private readonly SocialScanProvider _socialScanProvider;
    private readonly HyperSyncProvider _hypersyncProvider;
    private readonly HttpClient _httpClient;
    private readonly ILogger<Requests> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    public Requests(IOptions<EnvVariables> env,
        ILogger<Requests> logger,
        IMemoryCache cache)
    {
        _hypersyncRpc = new Web3(env.Value.HyperSyncRpc);
        _batchSize = env.Value.BatchSize;
        
        _socialScanProvider = new SocialScanProvider
        (baseUrl: env.Value.SocialScanApi,
            apiKey: env.Value.SocialScanPublicApiKey);
        _hypersyncProvider = new HyperSyncProvider(
            baseUrl: env.Value.HyperSyncApi);
        
        _httpClient = new HttpClient();
        _logger = logger;
        _cache = cache;
    }
    
    public async Task<long> GetCurrentBlockAsync()
    {
        var current = await _hypersyncRpc.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        
        return (long)current.Value;
    }

    public async Task<IReadOnlyList<BlockWithTransactions>> GetLastBlockWithTransactionAsync(BigInteger blockNumber)
    {
        int maxOffset = (int)BigInteger.Min(blockNumber, _batchSize);
        
        var blockParams = Enumerable
            .Range(0, maxOffset + 1)
            .Select(i => new HexBigInteger(blockNumber - i))
            .ToArray();

        var blocks = (await _hypersyncRpc.Eth.Blocks
                .GetBlockWithTransactionsByNumber
                .SendBatchRequestAsync(blockParams))
            .OrderBy(b => b.Number.Value).ToList();

        return blocks;
    }

    public async Task<IReadOnlyList<DailyNetworkUtilization>?> GetDailyNetworkUtilizationAsync(int range)
    {
        var cacheKey = $"{nameof(GetDailyNetworkUtilizationAsync)}:{range}";

        if (_cache.TryGetValue(cacheKey, out List<DailyNetworkUtilization>? cached))
        {
            return cached;
        }
        
        var uri = _socialScanProvider
            .WithModule(Module.Stats)
            .WithAction(Action.DailyNetUtilization)
            .WithStartDate(DateTime.UtcNow.AddDays(-range))
            .WithEndDate(DateTime.UtcNow)
            .Build();

        var response = await _httpClient.GetFromJsonAsync<SocialScanResponse<List<DailyNetworkUtilization>>>(uri);
        
        if(response == null || response.Status != "1")
            return null;

        _cache.Set(
            cacheKey,
            response.Result,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Priority = CacheItemPriority.Normal
            });
        
        return response.Result;
    }

    public async Task<IReadOnlyList<DailyTransactionFee>?> GetDailyNetworkTransactionFeeAsync(int range)
    {
        var cacheKey = $"{nameof(GetDailyNetworkTransactionFeeAsync)}:{range}";

        if (_cache.TryGetValue(cacheKey, out List<DailyTransactionFee>? cached))
        {
            return cached;
        }
        
        var uri = _socialScanProvider
            .WithModule(Module.Stats)
            .WithAction(Action.DailyTxnFee)
            .WithStartDate(DateTime.UtcNow.AddDays(-range))
            .WithEndDate(DateTime.UtcNow)
            .Build();

        var response = await _httpClient.GetFromJsonAsync<SocialScanResponse<List<DailyTransactionFee>>>(uri);
        
        if(response == null || response.Status != "1")
            return null;

        _cache.Set(
            cacheKey,
            response.Result,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Priority = CacheItemPriority.Normal
            });
        
        return response.Result;
    }
    
    public async Task<IReadOnlyList<DailyTransactionCount>?> GetDailyTransactionCountAsync(int range)
    {
        var cacheKey = $"{nameof(GetDailyTransactionCountAsync)}:{range}";

        if (_cache.TryGetValue(cacheKey, out List<DailyTransactionCount>? cached))
        {
            return cached;
        }
        
        var uri = _socialScanProvider
            .WithModule(Module.Stats)
            .WithAction(Action.DailyTx)
            .WithStartDate(DateTime.UtcNow.AddDays(-range))
            .WithEndDate(DateTime.UtcNow)
            .Build();

        var response = await _httpClient.GetFromJsonAsync<SocialScanResponse<List<DailyTransactionCount>>>(uri);
        
        if(response == null || response.Status != "1")
            return null;

        _cache.Set(
            cacheKey,
            response.Result,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Priority = CacheItemPriority.Normal
            });
        
        return response.Result;
    }

    public async Task<HypersyncTransaction> HypersyncTransactionAsync(long start, long end)
    {
        var uri = _hypersyncProvider
            .WithStartBlock(start)
            .WithEndBlock(end)
            .Build();

        var response = await _httpClient.GetFromJsonAsync<HypersyncTransaction>(uri)
                       ?? new HypersyncTransaction(0, 0, 0);
        return response;
    }

    public async Task<IReadOnlyList<Transaction>> GetLatestBlockTransaction(int page, int pageSize = 20)
    {
        var cacheKey = $"{nameof(GetLatestBlockTransaction)}";

        if (_cache.TryGetValue(cacheKey, out List<Transaction>? blockData))
        {
            _logger.LogCritical("Getting data from cache");
            return blockData?
                       .Skip((page -1) * pageSize)
                       .Take(pageSize * page)
                       .ToList()
                ?? Enumerable.Empty<Transaction>().ToList();
        }
        
        var latestBlock = await _hypersyncRpc.Eth.Blocks
            .GetBlockNumber.SendRequestAsync();

        BlockWithTransactions data = new();
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                data = await _hypersyncRpc.Eth
                    .Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(latestBlock);
            }
            catch (Nethereum.JsonRpc.Client.RpcResponseException ex)
                when (ex.Message.Contains("greater than latest block"))
            {
                if (attempt == 3)
                    return Enumerable.Empty<Transaction>().ToList();
                
                await Task.Delay(20);
            }
        }

        var transactions = data.Transactions.Select(tx => new Transaction(
            TxHash: tx.TransactionHash,
            From: tx.From,
            To: tx.To,
            ValueWei: tx.Value.Value.ToString(),
            Timestamp: DateTimeOffset
                .FromUnixTimeSeconds((long)data.Timestamp.Value)
                .UtcDateTime
            )).ToList();

        _cache.Set(
            cacheKey,
            transactions,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
                SlidingExpiration = TimeSpan.FromSeconds(5),
                Priority = CacheItemPriority.High
            });
        
        _logger.LogCritical("Getting data from request");
        return transactions
            .Skip((page -1) * pageSize)
            .Take(pageSize * page)
            .ToList();
    }

    public async Task<IReadOnlyList<Block>> GetLatestBlockData()
    {
        var cacheKey = $"{nameof(GetLatestBlockData)}";

        if (_cache.TryGetValue(cacheKey, out List<Block>? blockData))
        {
            _logger.LogCritical("Getting data from cache");
            return blockData ??  Enumerable.Empty<Block>().ToList();
        }

        var latestBlock = await _hypersyncRpc.Eth.Blocks
            .GetBlockNumber.SendRequestAsync();
        int maxOffset = (int)BigInteger.Min(latestBlock, _batchSize);
        
        var blockParams = Enumerable
            .Range(0, maxOffset + 1)
            .Select(i => new HexBigInteger(latestBlock.Value - i))
            .ToArray();

        var blockWithTransactions = (await _hypersyncRpc.Eth.Blocks
                .GetBlockWithTransactionsByNumber
                .SendBatchRequestAsync(blockParams))
            .ToList();

        var blocks = blockWithTransactions.Select(b => new Block(
            Height: (long)b.Number.Value,
            Hash: b.BlockHash,
            TxAmount: b.TransactionCount(),
            GasUsed: b.GasUsed.Value.ToString(),
            GasLimit: b.GasLimit.Value.ToString(),
            Timestamp: DateTimeOffset
                .FromUnixTimeSeconds((long)b.Timestamp.Value)
                .UtcDateTime
        )).ToList();
        
        _cache.Set(
            cacheKey,
            blocks,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
                SlidingExpiration = TimeSpan.FromSeconds(5),
                Priority = CacheItemPriority.High
            });
        
        _logger.LogCritical("Getting data from request");
        
        return blocks;
    }

    public async Task<int> GetDaysAfterCreatingAsync()
    {
        var block = await _hypersyncRpc.Eth.Blocks
            .GetBlockWithTransactionsByNumber
            .SendRequestAsync(new BlockParameter(new HexBigInteger(1)));

        var blockDateTime = DateTimeOffset
            .FromUnixTimeSeconds((long)block.Timestamp.Value);
        
        var diff = DateTime.UtcNow - blockDateTime;
        
        return diff.Days + 1;
    }

    public async Task<long> GetBlockByTimestampAsync(DateTime? time)
    {
        var latestNumber = await _hypersyncRpc.Eth.Blocks
            .GetBlockNumber
            .SendRequestAsync()
            .ConfigureAwait(false);
        BigInteger low = 0;
        BigInteger high = latestNumber.Value;

        while (low <= high)
        {
            BigInteger mid = (low + high) / 2;
            
            var block = await  _hypersyncRpc.Eth.Blocks
                .GetBlockWithTransactionsByNumber
                .SendRequestAsync(new BlockParameter(new HexBigInteger(mid)))
                .ConfigureAwait(false);
            
            var blockTimeUtc = DateTimeOffset
                .FromUnixTimeSeconds((long)block.Timestamp.Value);

            if (blockTimeUtc <= time)
                low = mid + 1;
            else
                high = mid - 1;
        }

        return (long)high;
    }
}
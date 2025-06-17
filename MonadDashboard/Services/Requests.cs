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
    private static readonly string TransactionCache = nameof(TransactionCache);
    private static readonly string BlockCache = nameof(BlockCache);
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

        if (range <= 0)
            range = await GetDaysAfterCreatingAsync();
        
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
        
        if (range <= 0)
            range = await GetDaysAfterCreatingAsync();
        
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
        
        if (range <= 0)
            range = await GetDaysAfterCreatingAsync();
        
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
        if (_cache.TryGetValue(TransactionCache, out List<Transaction>? blockData)
            && blockData != null)
        {
            return blockData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        
        var latestBlock = await _hypersyncRpc.Eth.Blocks
            .GetBlockNumber.SendRequestAsync();
        
        int maxOffset = (int)BigInteger.Min(latestBlock, _batchSize * 2);
        
        var blockParams = Enumerable
            .Range(0, maxOffset + 1)
            .Select(i => new HexBigInteger(latestBlock.Value - i))
            .ToArray();

        List<BlockWithTransactions> rawData = new();

        for (int i = 0; i < blockParams.Length; i += _batchSize)
        {
            var batch = blockParams
                .Skip(i)
                .Take(_batchSize)
                .ToArray();
            
            var block = await _hypersyncRpc.Eth.Blocks
                .GetBlockWithTransactionsByNumber
                .SendBatchRequestAsync(batch);
            
            rawData.AddRange(block);
            await Task.Delay(20);
        }

        var response = rawData
            .SelectMany(block => block.Transactions.Select(tx => 
                new Transaction(
                    TxHash:    tx.TransactionHash,
                    From:      tx.From,
                    To:        tx.To,
                    ValueWei:  tx.Value.Value.ToString(),
                    Timestamp: DateTimeOffset
                        .FromUnixTimeSeconds((long)block.Timestamp.Value)
                        .UtcDateTime
                )
            ))
            .ToList();

        _cache.Set(
            TransactionCache,
            response,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(50),
                SlidingExpiration = TimeSpan.FromSeconds(35),
                Priority = CacheItemPriority.High
            });
        
        return response
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<IReadOnlyList<Block>> GetLatestBlockData(int page, int pageSize = 20)
    {
        if (_cache.TryGetValue(BlockCache, out List<Block>? blockData)
            && blockData != null)
        {
            return blockData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        
        var latestBlock = await _hypersyncRpc.Eth.Blocks
            .GetBlockNumber.SendRequestAsync();
        
        int maxOffset = (int)BigInteger.Min(latestBlock, _batchSize * 3);
        
        var blockParams = Enumerable
            .Range(0, maxOffset + 1)
            .Select(i => new HexBigInteger(latestBlock.Value - i))
            .ToArray();

        List<BlockWithTransactions> rawData = new();

        for (int i = 0; i < blockParams.Length; i += _batchSize)
        {
            var batch = blockParams
                .Skip(i)
                .Take(_batchSize)
                .ToArray();

            _logger.LogDebug($"Requesting batch blocks: {batch.Length}");
            
            var block = await _hypersyncRpc.Eth.Blocks
                .GetBlockWithTransactionsByNumber
                .SendBatchRequestAsync(batch);
            
            rawData.AddRange(block);
            await Task.Delay(20);
        }

        var response = rawData.Select(b => new Block(
            Height: (long)b.Number.Value,
            Hash: b.BlockHash.ToString(),
            TxAmount: b.TransactionCount(),
            GasUsed: b.GasUsed.Value.ToString(),
            GasLimit: b.GasLimit.Value.ToString(),
            Timestamp: DateTimeOffset
                .FromUnixTimeSeconds((long)b.Timestamp.Value)
                .UtcDateTime
        ))
        .ToList();

        _cache.Set(
            BlockCache,
            response,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(50),
                SlidingExpiration = TimeSpan.FromSeconds(35),
                Priority = CacheItemPriority.High
            });
        
        return response
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
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

    public async Task<IReadOnlyList<TransactionReceipt>> GetBlockTransactionsReceiptsAsync(BigInteger blockNumber)
    {
        var block = await _hypersyncRpc.Eth.Blocks
            .GetBlockWithTransactionsByNumber
            .SendRequestAsync(new HexBigInteger(blockNumber));
        
        var transactionHashes = block.Transactions
            .Select(tx => tx.TransactionHash.ToString())
            .ToArray();
        
        var receipt = new List<TransactionReceipt>();

        for (int i = 0; i < transactionHashes.Length; i += _batchSize)
        {
            var txHashSelection = transactionHashes
                .Skip(i)
                .Take(_batchSize)
                .ToArray();
            
            var tmp = await _hypersyncRpc.Eth.Transactions
                .GetTransactionReceipt
                .SendBatchRequestAsync(txHashSelection);
            
            receipt.AddRange(tmp);
            await Task.Delay(20);
        }

        return receipt;
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
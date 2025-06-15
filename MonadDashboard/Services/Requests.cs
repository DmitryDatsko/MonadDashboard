using System.Numerics;
using Microsoft.Extensions.Options;
using MonadDashboard.Configuration;
using MonadDashboard.Models.Responses;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Action = MonadDashboard.Models.SocialScan.Action;
using Module = MonadDashboard.Models.SocialScan.Module;

namespace MonadDashboard.Services;

public class Requests : IRequests
{
    private readonly Web3 _hypersyncRpc;
    private readonly int _batchSize;
    private readonly SocialScanProvider _socialScanProvider;
    private readonly HttpClient _httpClient;
    private readonly ILogger<Requests> _logger;
    public Requests(IOptions<EnvVariables> env,
        ILogger<Requests> logger)
    {
        _hypersyncRpc = new Web3(env.Value.HyperSyncRpc);
        _batchSize = env.Value.BatchSize;
        _socialScanProvider = new SocialScanProvider
        (baseUrl: env.Value.SocialScanApi,
            apiKey: env.Value.SocialScanPublicApiKey);
        _httpClient = new HttpClient();
        _logger = logger;
    }
    
    public async Task<BigInteger> GetCurrentBlockAsync()
    {
        var current = await _hypersyncRpc.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        
        return current.Value;
    }

    public async Task<IReadOnlyList<BlockWithTransactions>> GetLastBlockWithTransaction(BigInteger blockNumber)
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

    public async Task<IReadOnlyList<DailyNetworkUtilization>?> GetDailyNetworkUtilization(int range)
    {
        var uri = _socialScanProvider
            .WithModule(Module.Stats)
            .WithAction(Action.DailyNetUtilization)
            .WithStartDate(DateTime.UtcNow.AddDays(-range))
            .WithEndDate(DateTime.UtcNow)
            .Build();

        var response = await _httpClient.GetFromJsonAsync<SocialScanResponse<List<DailyNetworkUtilization>>>(uri);
        
        if(response == null || response.Status != "1")
            return null;
        
        return response.Result;
    }

    public async Task<IReadOnlyList<DailyTransactionFee>?> GetDailyNetworkTransactionFee(int range)
    {
        var uri = _socialScanProvider
            .WithModule(Module.Stats)
            .WithAction(Action.DailyTxnFee)
            .WithStartDate(DateTime.UtcNow.AddDays(-range))
            .WithEndDate(DateTime.UtcNow)
            .Build();

        var response = await _httpClient.GetFromJsonAsync<SocialScanResponse<List<DailyTransactionFee>>>(uri);
        
        if(response == null || response.Status != "1")
            return null;
        
        return response.Result;
    }
    
    public async Task<IReadOnlyList<DailyTransactionCount>?> GetDailyTransactionCount(int range)
    {
        var uri = _socialScanProvider
            .WithModule(Module.Stats)
            .WithAction(Action.DailyTx)
            .WithStartDate(DateTime.UtcNow.AddDays(-range))
            .WithEndDate(DateTime.UtcNow)
            .Build();

        var response = await _httpClient.GetFromJsonAsync<SocialScanResponse<List<DailyTransactionCount>>>(uri);
        
        if(response == null || response.Status != "1")
            return null;
        
        return response.Result;
    }

    public async Task<int> GetDaysAfterCreating()
    {
        var block = await _hypersyncRpc.Eth.Blocks
            .GetBlockWithTransactionsByNumber
            .SendRequestAsync(new BlockParameter(new HexBigInteger(1)));

        var blockDateTime = DateTimeOffset
            .FromUnixTimeSeconds((long)block.Timestamp.Value);
        
        var diff = DateTime.UtcNow - blockDateTime;
        
        return diff.Days;
    }

    public async Task<long> GetBlockByTimestamp(DateTime? time)
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
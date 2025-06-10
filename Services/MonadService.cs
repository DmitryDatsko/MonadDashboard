using System.Numerics;
using Microsoft.Extensions.Options;
using MonadDashboard.Configuration;
using MonadDashboard.Extensions;
using MonadDashboard.Models.DTO;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace MonadDashboard.Services;

public class MonadService(IOptions<EnvVariables> env) : IMonadService
{
    private readonly int _batchSize = env.Value.BatchSize;
    private readonly Web3 _web3 = new(env.Value.RpcUrl);
    
    public async Task<Overview> GetOverview()
    {
        var latestBlockNumber = (long)(await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;

        var blockParams = Enumerable.Range(0, _batchSize + 1)
            .Select(i => new HexBigInteger(latestBlockNumber - i))
            .ToArray();
        
        var blocks = (await _web3.Eth.Blocks
                .GetBlockWithTransactionsByNumber
                .SendBatchRequestAsync(blockParams))
            .OrderBy(b => b.Number.Value);
        
        var ordered = blocks
            .OrderBy(b => b.Number.Value)
            .ToList();

        return new Overview
        {
            CurrentBlock = latestBlockNumber,
            AvgBlockTime = AvgBlockTime(ordered),
            Transactions = ordered[0].TransactionCount(),
            AvgGasPrice = AvgGasPrice(ordered)
        };
    }

    private double AvgBlockTime(List<BlockWithTransactions> blocks)
    {
        var times = blocks
            .Select(b => DateTimeOffset.FromUnixTimeSeconds((long)b.Timestamp.Value))
            .ToList();

        var spans = times
            .Zip(times.Skip(1), (prev, next) => next - prev);
        
        double avgTicks = spans.Average(span => span.Ticks);
        var avgSpan = TimeSpan.FromTicks((long)avgTicks);
        
        return avgSpan.TotalSeconds;
    }

    private decimal AvgGasPrice(List<BlockWithTransactions> blocks)
    {
        var gasPrices = blocks
            .SelectMany(b => b.Transactions)
            .Select(tx => tx.GasPrice.Value * tx.Gas.Value)
            .ToList();
        
        BigInteger avgGasPriceWei = gasPrices.Sum() / gasPrices.Count;
        
        return Web3.Convert.FromWei(avgGasPriceWei);
    }
}
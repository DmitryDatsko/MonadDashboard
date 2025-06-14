using System.Numerics;
using Microsoft.Extensions.Options;
using MonadDashboard.Configuration;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace MonadDashboard.Services;

public class Requests : IRequests
{
    private readonly Web3 _hypersyncRpc;
    private readonly int _batchSize;

    public Requests(IOptions<EnvVariables> env)
    {
        _hypersyncRpc = new Web3(env.Value.HyperSyncRpc);
        _batchSize = env.Value.BatchSize;
        Console.WriteLine(_batchSize);
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
}
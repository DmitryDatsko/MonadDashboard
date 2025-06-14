using System.Numerics;
using Nethereum.RPC.Eth.DTOs;

namespace MonadDashboard.Services;

public interface IRequests
{
    Task<BigInteger> GetCurrentBlockAsync();
    Task<IReadOnlyList<BlockWithTransactions>> GetLastBlockWithTransaction(BigInteger blockNumber);
}
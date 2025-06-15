using System.Numerics;
using MonadDashboard.Models.Responses;
using Nethereum.RPC.Eth.DTOs;

namespace MonadDashboard.Services;

public interface IRequests
{
    Task<BigInteger> GetCurrentBlockAsync();
    Task<IReadOnlyList<BlockWithTransactions>> GetLastBlockWithTransaction(BigInteger blockNumber);
    Task<IReadOnlyList<DailyNetworkUtilization>?> GetDailyNetworkUtilization(int range);
    Task<IReadOnlyList<DailyTransactionFee>?> GetDailyNetworkTransactionFee(int range);
    Task<IReadOnlyList<DailyTransactionCount>?> GetDailyTransactionCount(int range);
    Task<int> GetDaysAfterCreating();
    Task<long> GetBlockByTimestamp(DateTime? time);
}
using System.Numerics;
using MonadDashboard.Models.Responses;
using Nethereum.RPC.Eth.DTOs;
using Block = MonadDashboard.Models.Responses.Block;
using Transaction = MonadDashboard.Models.Responses.Transaction;

namespace MonadDashboard.Services;

public interface IRequests
{
    Task<long> GetCurrentBlockAsync();
    Task<IReadOnlyList<BlockWithTransactions>> GetLastBlockWithTransactionAsync(BigInteger blockNumber);
    Task<IReadOnlyList<DailyNetworkUtilization>?> GetDailyNetworkUtilizationAsync(int range);
    Task<IReadOnlyList<DailyTransactionFee>?> GetDailyNetworkTransactionFeeAsync(int range);
    Task<IReadOnlyList<DailyTransactionCount>?> GetDailyTransactionCountAsync(int range);
    Task<HypersyncTransaction> HypersyncTransactionAsync(long start, long end);
    Task<IReadOnlyList<Transaction>> GetLatestBlockTransaction(int page, int pageSize = 20);
    Task<IReadOnlyList<Block>> GetLatestBlockData(int page, int pageSize = 20);
    Task<int> GetDaysAfterCreatingAsync();
    Task<IReadOnlyList<TransactionReceipt>> GetBlockTransactionsReceiptsAsync(BigInteger blockNumber);
    Task<IReadOnlyList<FilterLog>> GetTransferLogsAsync(BigInteger blockNumber);
    Task<long> GetBlockByTimestampAsync(DateTime? time);
}
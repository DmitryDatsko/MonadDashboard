namespace MonadDashboard.Models.DTO;

public class Overview
{
    public long CurrentBlock { get; set; }
    public double AvgBlockTime { get; set; }
    public long Transactions { get; set; }
    public decimal AvgGasPrice { get; set; }
}

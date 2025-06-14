using System.Numerics;
using System.Transactions;

namespace MonadDashboard.Extensions;

public static class NethereumExtensions
{
    public static BigInteger Sum(this IEnumerable<BigInteger> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return source.Aggregate(BigInteger.Zero, (acc, v) => acc + v);
    }
}

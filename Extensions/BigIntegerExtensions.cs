using System.Numerics;

namespace MonadDashboard.Extensions;

public static class BigIntegerExtensions
{
    public static BigInteger Sum(this IEnumerable<BigInteger> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return source.Aggregate(BigInteger.Zero, (acc, v) => acc + v);
    }
}

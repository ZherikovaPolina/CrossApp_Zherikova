namespace Core.Extensions;

public static class EnumerableExtensions
{
    public static IReadOnlyList<T> TopBy<T>(
        this IEnumerable<T> source,
        Func<T, decimal> keySelector,
        int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        if (count <= 0)
            return Array.Empty<T>();

        return source
            .OrderByDescending(keySelector)
            .Take(count)
            .ToList();
    }
}
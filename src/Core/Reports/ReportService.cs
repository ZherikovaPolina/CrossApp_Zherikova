using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    public static IReadOnlyList<CustomerSummary> TopCustomersByTotal(
        IEnumerable<Order> source,
        int n) =>
        source
            .GroupBy(
                order => order.CustomerId,
                StringComparer.Ordinal)
            .Select(group => new CustomerSummary(
                group.Key,
                group.Count(),
                group.Sum(order => order.Total)))
            .OrderByDescending(customer => customer.Total)
            .ThenBy(
                customer => customer.CustomerId,
                StringComparer.Ordinal)
            .Take(n)
            .ToList();

    public static IReadOnlyList<OrderReportLine> ExpensiveOrders(
        IEnumerable<Order> source,
        decimal threshold) =>
        source
            .Where(order => order.Total > threshold)
            .OrderByDescending(order => order.Total)
            .ThenBy(
                order => order.Id,
                StringComparer.Ordinal)
            .Select(order => new OrderReportLine(
                order.Id,
                order.CustomerId,
                order.Total))
            .ToList();

    public static IReadOnlyList<GroupSummary> ByStatus(
        IEnumerable<Order> source) =>
        source
            .GroupBy(order => order.Status)
            .Select(group => new GroupSummary(
                group.Key,
                group.Count(),
                group.Sum(order => order.Total)))
            .OrderByDescending(summary => summary.Total)
            .ThenBy(summary => summary.Status)
            .ToList();

    public static IEnumerable<Order> OrdersBelowTotal(
        IEnumerable<Order> source,
        decimal threshold) =>
        source.Where(order => order.Total < threshold);

    public static (
        int Items,
        decimal Total,
        decimal Average,
        decimal Max) Totals(
        IEnumerable<Order> source)
    {
        var list = source.ToList();

        return list.Count == 0
            ? (0, 0m, 0m, 0m)
            : (
                list.Count,
                list.Sum(order => order.Total),
                list.Average(order => order.Total),
                list.Max(order => order.Total));
    }
}
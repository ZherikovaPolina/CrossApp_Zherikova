using Core.Domain;
using Core.Extensions;

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
            .OrderBy(
                customer => customer.CustomerId,
                StringComparer.Ordinal)
            .TopBy(
                customer => customer.Total,
                n);

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

    public static IReadOnlyList<GroupSummary> ByStatusQuerySyntax(
        IEnumerable<Order> source)
    {
        var query =
            from order in source
            group order by order.Status into statusGroup
            let total =
                statusGroup.Sum(order => order.Total)
            orderby total descending,
                statusGroup.Key
            select new GroupSummary(
                statusGroup.Key,
                statusGroup.Count(),
                total);

        return query.ToList();
    }

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

    public static IReadOnlyList<ProductOrdersSummary> ProductOrders(
        IEnumerable<Product> products,
        IEnumerable<OrderLine> orderLines) =>
        products
            .GroupJoin(
                orderLines,
                product => product.Id,
                line => line.ProductId,
                (product, lines) => new ProductOrdersSummary(
                    product.Id,
                    product.Name,
                    product.AvailableQuantity,
                    lines.Sum(line => line.Quantity),
                    lines.Sum(line =>
                        line.Price * line.Quantity)),
                StringComparer.Ordinal)
            .OrderByDescending(
                summary => summary.OrderedTotal)
            .ThenBy(
                summary => summary.ProductId,
                StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<ProductOrdersSummary> TopProducts(
        IEnumerable<ProductOrdersSummary> source,
        int n) =>
        source
            .OrderBy(
                product => product.ProductId,
                StringComparer.Ordinal)
            .TopBy(
                product => product.OrderedTotal,
                n);
}
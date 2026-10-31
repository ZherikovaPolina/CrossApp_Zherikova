namespace Core.Reports;

public sealed record ProductOrdersSummary(
    string ProductId,
    string ProductName,
    int AvailableQuantity,
    int OrderedQuantity,
    decimal OrderedTotal);
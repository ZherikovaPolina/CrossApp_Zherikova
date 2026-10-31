using Core.Domain;

namespace Core.Reports;

public sealed record GroupSummary(
    OrderStatus Status,
    int Count,
    decimal Total);
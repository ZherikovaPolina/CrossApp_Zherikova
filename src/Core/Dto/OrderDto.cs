using Core.Domain;

namespace Core.Dto;

public record OrderDto(
    string Id,
    string CustomerId,
    OrderStatus Status,
    IReadOnlyList<OrderLineDto> Lines);
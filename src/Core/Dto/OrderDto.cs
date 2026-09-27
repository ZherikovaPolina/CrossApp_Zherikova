namespace Core.Dto;

public record OrderDto(
    string Id,
    string CustomerId,
    bool IsConfirmed,
    IReadOnlyList<OrderLineDto> Lines);
using Core.Domain;
using Core.Dto;

namespace Api.Contracts;

public sealed record OrderDto(
    string Id,
    string CustomerId,
    OrderStatus Status,
    IReadOnlyList<OrderLineDto> Lines,
    DateTimeOffset? CreatedAt)
{
    public static OrderDto FromDomain(Order order)
    {
        var dto = order.ToDto();

        return new OrderDto(
            dto.Id,
            dto.CustomerId,
            dto.Status,
            dto.Lines,
            dto.CreatedAt);
    }
}
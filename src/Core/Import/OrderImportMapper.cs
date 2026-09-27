using Core.Domain;
using Core.Dto;

namespace Core.Import;

public static class OrderImportMapper
{
    public static ImportResult<Order> ToDomain(
        ImportResult<OrderDto> importResult)
    {
        ArgumentNullException.ThrowIfNull(importResult);

        var orders = new List<Order>();
        var errors = new List<string>(importResult.Errors);

        for (int i = 0; i < importResult.Items.Count; i++)
        {
            OrderDto dto = importResult.Items[i];

            try
            {
                Order order = Order.FromDto(dto);
                orders.Add(order);
            }
            catch (ArgumentException ex)
            {
                errors.Add(
                    $"запис {i + 1}: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(
                    $"запис {i + 1}: {ex.Message}");
            }
        }

        return new ImportResult<Order>(
            orders.AsReadOnly(),
            errors.AsReadOnly());
    }
}
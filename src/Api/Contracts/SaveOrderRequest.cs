namespace Api.Contracts;

public sealed record SaveOrderLineRequest(
    string? ProductId,
    string? Name,
    decimal Price,
    int Quantity);

public sealed record SaveOrderRequest(
    string? Customer,
    DateTimeOffset CreatedAt,
    List<SaveOrderLineRequest?>? Lines)
{
    public bool TryValidate(
        out Dictionary<string, string[]> errors)
    {
        var found = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(Customer) ||
            Customer.Length > 100)
        {
            found[nameof(Customer)] =
                ["Клієнт обов'язковий, до 100 символів."];
        }

        if (CreatedAt == default)
        {
            found[nameof(CreatedAt)] =
                ["Дата створення обов'язкова."];
        }

        if (Lines is null || Lines.Count == 0)
        {
            found[nameof(Lines)] =
                ["Замовлення повинно містити хоча б один товар."];
        }
        else
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];

                if (line is null)
                {
                    found[$"Lines[{i}]"] =
                        ["Позиція замовлення не може бути порожньою."];
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line.ProductId) ||
                    line.ProductId.Length > 50)
                {
                    found[$"Lines[{i}].ProductId"] =
                        ["Ідентифікатор товару обов'язковий, до 50 символів."];
                }

                if (string.IsNullOrWhiteSpace(line.Name) ||
                    line.Name.Length > 200)
                {
                    found[$"Lines[{i}].Name"] =
                        ["Назва товару обов'язкова, до 200 символів."];
                }

                if (line.Price <= 0)
                {
                    found[$"Lines[{i}].Price"] =
                        ["Ціна повинна бути більшою за нуль."];
                }

                if (line.Quantity <= 0)
                {
                    found[$"Lines[{i}].Quantity"] =
                        ["Кількість повинна бути більшою за нуль."];
                }
            }
        }

        errors = found;
        return found.Count == 0;
    }
}

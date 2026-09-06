# CrossApp
Наскрізний проєкт з крос-платформного програмування.

Предметна область: Замовлення. Сутності: Customer, Product, Order, OrderLine.

Призначення: оформлення замовлень і підрахунок сум.

## Запуск
```bash
dotnet build 
dotnet run --project src/Cli
```

## Середовище
.NET SDK 10.0.400, Unix 15.6.1, ARM64.

### Порівняння self-contained публікацій
RID

osx-arm64 = 76 MB 

linux-x64 = 71 MB 

Публікація для linux-x64 на 5 MB меншою за публікацію для osx-arm64.

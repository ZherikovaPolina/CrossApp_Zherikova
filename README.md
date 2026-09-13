# CrossApp
Наскрізний проєкт з крос-платформного програмування.

Предметна область: Замовлення. Сутності: Customer, Product, Order, OrderLine.

Призначення: оформлення замовлень і підрахунок сум.

## Структура solution
```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs
```
Залежність між проєктами:

Cli → Core

У проєкті Core на семестр заплановані каталоги:
Dto/ — record-типи формату даних;

Domain/ — сутності з поведінкою та інваріантами;

Storage/ — реалізації сховищ.

## Запуск
Збірка solution:
```bash
dotnet build 
```
Запуск консольного застосунку:
```bash
dotnet run --project src/Cli
```
Запуск із виведенням інформації у форматі JSON
```bash
dotnet run --project src/Cli -- --json
```
Збірка бібліотеки Core:
```bash
dotnet build src/Core/Core.csproj
```
Запуск self-contained версії з каталогу publish:
```bash
cd src/Cli/bin/Release/net10.0/osx-arm64/publish/
./Cli
```
## Publish
### Framework-dependent
```bash
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained false
```

У цьому режимі .NET runtime не входить до публікації, тому на комп'ютері повинен бути встановлений сумісний runtime.

Розмір publish: 172K.

### Self-contained
```bash
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true
```

У цьому режимі .NET runtime входить до публікації, тому окремо встановлювати .NET на комп'ютері не потрібно.

Розмір publish: 83M.

## Середовище
.NET SDK 10.0.400

 Unix 15.6.1
 
 Архітектура: ARM64

 RID: osx-arm64
 
 .NET runtime: 10.0.11



### Порівняння self-contained публікацій
RID

osx-arm64 = 76 MB 

linux-x64 = 71 MB 

Публікація для linux-x64 на 5 MB меншою за публікацію для osx-arm64.

### Порівняння publish
| RID | Режим | Розмір publish | Чи потрібен встановлений runtime |
|---|---|---:|---|
| osx-arm64 | framework-dependent | 172K | так (.NET 10) |
| osx-arm64 | self-contained | 83M | ні |

Self-contained версія є набагато більшою, оскільки, окрім програми, містить ще й .NET runtime.

## Multi-targeting

Бібліотека Core збирається для двох цільових платформ:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

Перевірка збірки:
```bash
dotnet build src/Core/Core.csproj
```
У результаті створюються збірки для net8.0 та net10.0.

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
Запуск із CSV-файлом:
```bash
dotnet run --project src/Cli -- data/sample.csv
```
Запуск із JSON-файлом:
```bash
dotnet run --project src/Cli -- data/sample.json
```
Запуск змішаного CSV:
```bash
dotnet run --project src/Cli -- data/mixed.csv
```
Перевірка неіснуючого файлу:
```bash
dotnet run --project src/Cli -- data/not-found.csv
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

### SingleFile

Публікація одним виконуваним файлом:

```bash
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ./publish-single
```
У каталозі publish створюється 3 файли, основний виконуваний файл Cli має розмір 76 MB.
Запуск:
```bash
./publish-single/Cli
```

### PublishTrimmed
Публікація з trimming:
```bash
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishTrimmed=true -o ./publish-trimmed
```
Розмір каталогу publish-trimmed становить 20 MB.
Під час збірки отримано 2 попередження IL2026, пов'язані з використанням:

JsonSerializer.Serialize(report, jsonOptions)

Попередження повідомляють, що під час trimming JSON-серіалізація може використовувати типи, які не вдається визначити під час аналізу.
Trimming може видаляти код, який система вважає невикористовуваним. Це може створити проблеми для коду, який використовує reflection або динамічне визначення типів.

Trimmed-версія успішно запускається:
```bash
./publish-trimmed/Cli
```

### Conditional compilation
Для перевірки різних цільових версій використовувалися команди:
```bash
dotnet run --project src/Cli -f net8.0
```
Результат:

Примітка збірки : збірка під net8.0

Для .NET 10:
```bash
dotnet run --project src/Cli -f net10.0
```
Результат:

Примітка збірки : збірка під net10.0

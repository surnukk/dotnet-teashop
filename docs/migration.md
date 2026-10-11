1. SQL: міграції та опис змін
Як це працює в EF Core
У каталог (Проєкт №2) реалізується через EF Core Code First. Там схема описується класами C# і Fluent API, а зміни фіксуються міграціями:
- Кожна міграція це клас із двома методами: Up (застосувати зміну) і Down (відкотити її).
- Міграції створюються командою dotnet ef migrations add <Назва>, а застосовуються командою dotnet ef database update.

Приклад того, як виглядатиме додавання нового поля через bash:
dotnet ef migrations add AddProductSku
dotnet ef database update


EastLeaf_OrderDB (Проєкт №1)
Для цієї бази використовуються ADO.NET і Dapper, тобто EF-міграцій не буде, і зміни схеми далі ведуться SQL-скриптами.
| Версія | Зміни |
|---|---|
| v1 | Таблиці Customer, Promotions, Products (локальна копія каталогу), Orders, OrderItems, PaymentRecords. Зв'язки 1:1 (Orders і PaymentRecords), 1:N (Customer і Orders), M:N (Orders і Products через OrderItems). Представлення vw_OrderSummary |
| v2 | Первинні ключі змінено на bigint identity. Додано CHECK, DEFAULT, UNIQUE і каскади (cascade для OrderItems і PaymentRecords, без cascade для клієнта). Додано індекси під конкретні запити, зокрема фільтровані |
| v3 | Додано CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted для Customer, Promotions, Orders. Додано rowversion в Orders. Додано фільтровані індекси для «живих» записів. Представлення ховає видалені замовлення |

---------------------------------------------------------------
2. MongoDB: контроль схеми та міграції
JSON Schema Validation
MongoDB за замовчуванням приймає будь-який документ. Помилка в коді може записати відгук без productId або з рейтингом 9, і база цього не помітить. JSON Schema Validation це аналог `NOT NULL` і `CHECK` із SQL: правила описуються на рівні колекції, а база відхиляє документи, які їх порушують.

Приклад правила для колекції reviews:
```javascript
db.createCollection("reviews", {
  validator: { $jsonSchema: {
    bsonType: "object",
    required: ["productId", "rating", "author"],
    properties: { rating: { bsonType: "int", minimum: 1, maximum: 5 } }
  } }
});
```

Скрипти міграцій
Хоча MongoDB не вимагає схеми, застосунок з часом змінюється: з'являються нові поля, змінюються назви. Старі документи залишаються зі старою структурою, тому їх потрібно оновлювати скриптами. Скрипт міграції має бути версіонованим і виконуватись лише один раз, для цього виконані версії записують у службову колекцію:
```javascript
migrate("001", "reviews: add helpfulVotes", function () {
  db.reviews.updateMany({ helpfulVotes: { $exists: false } }, { $set: { helpfulVotes: 0 } });
});
```
Тут migrate це допоміжна функція, яка перевіряє колекцію _migrations, виконує зміну, якщо цієї версії там ще немає, і записує її після виконання.
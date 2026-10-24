## 1. Лаб. 01: Бази Даних
## 1.1. Опис предметної області
Системи «East Leaf» є спеціалізованим інтернет-магазином преміального чаю, традиційного посуду для заварювання (гайвані, ісинські чайники, піали) та ексклюзивних подарункових наборів. Користувачам системи є покупці (любителі та поціновувачі чайних церемоній), які можуть переглядати каталог, збирати кошик оформлювати замовлення з вибором способів оплати та залишити відгуки чи обговорювати культуру заварювання. Система забезпечує повний цикл продажу: від презентації товару з урахуванням регіону походження та року збору до транзакційної обробки замовлення й накопичення відгуків. 


## 1.2. Закріплення технології персистентності за контекстами
Проєкт №1: Сервіс замовлень та клієнтів (EastLeaf_OrderDB), SQL Server + ADO.NET & Dapper
- Підхід: процедурний стиль, ручні SQL-запити, збережувані процедури.
- Обґрунтування: замовлення це транзакційний процес зі строгими правилами. Зміна статусу замовлення виконується в явній транзакції збереженою процедурою, яка відмовляє на недопустимому переході (наприклад, не можна відвантажити неоплачене замовлення). Створення замовлення разом із позиціями та платежем має виконуватись атомарно, а паралельні зміни одного рядка контролює rowversion. ADO.NET і Dapper дають повний контроль над SQL, транзакціями та викликом процедур.

Проєкт №2: Сервіс каталогу товарів (EastLeaf_CatalogDB), SQL Server + EF Core
- Підхід: Code First, Fluent API, міграції.
- Обґрунтування: каталог має багато сутностей і всі типи зв'язків: 1:1 (Products і ProductDetails, Products і BrewingGuides), 1:N (Categories і Products, Products і ProductImages), M:N (Products і Tags через ProductTags із полем AddedDate). EF Core зручно описує такі зв'язки через Fluent API, а міграції дозволяють версіонувати зміни схеми. Також він підтримує seed-дані для довідників (категорії, теги) і rowversion як токен оптимістичної конкурентності.

Проєкт №3: Сервіс відгуків та обговорень (EastLeaf_ReviewsDB), MongoDB
- Підхід: документна модель, орієнтація на агрегати.
- Обґрунтування: відгук разом з автором і коментарями читається як єдине ціле, тому вони вбудовуються в один документ (embedding). Обговорення можуть містити необмежену кількість повідомлень, тому зберігаються окремою колекцією й підключаються за посиланням (referencing). Структура документів різна (наприклад, не в усіх відгуків є фото), і гнучка схема MongoDB це дозволяє. Поле productName денормалізоване для швидкого читання без звернення до Каталогу.


## 1.3. Таблиця політики дублювання даних
| Поле | Де зберігається | Джерело (власник даних) | Політика узгодженості |
|---|---|---|---|
| OrderItems.ProductID | EastLeaf_OrderDB | Product (EastLeaf_CatalogDB) | Логічне посилання за ідентифікатором, без FK між базами |
| OrderItems.ProductName, UnitPrice | EastLeaf_OrderDB | Product (EastLeaf_CatalogDB) | Знімок на момент оформлення: не оновлюється |
| Products.ProductName, Price (локальна копія) | EastLeaf_OrderDB | Product (EastLeaf_CatalogDB) | Копія для читання: оновлюється подіями з Каталогу |
| reviews.productId | EastLeaf_ReviewsDB | Product (EastLeaf_CatalogDB) | Логічне посилання за ідентифікатором, без FK |
| reviews.productName | EastLeaf_ReviewsDB | Product (EastLeaf_CatalogDB) | Копія для читання: оновлюється подіями з Каталогу |
| reviews.author.userId, displayName | EastLeaf_ReviewsDB | Customer (EastLeaf_OrderDB) | userId = CustomerID, без FK; displayName це знімок на момент відгуку, не оновлюється |

Принцип: у кожного дубльованого поля є єдиний власник даних. Двох незалежно редагованих копій без власника (dual write) немає. ID товарів у seed-даних усіх трьох баз збігаються свідомо.


## 1.4. Розгортання з нуля
Порядок виконання в SSMS: відкрити файл, обрати з'єднання із сервером, натиснути Execute. Усі файли виконуються цілком, без змін.

4.1. Проєкт №1: EastLeaf_OrderDB (SQL Server)
1. Виконати db/p1/schema.sql. Скрипт сам видаляє стару версію бази (якщо вона є) і створює її наново, тому його можна запускати повторно. Він створює таблиці, зв'язки, обмеження, індекси, представлення vw_OrderSummary, службові колонки й rowversion. Увага: скрипт призначений для розробки, бо знищує дані бази.
2. Виконати db/p1/procedures.sql: створює 6 збережуваних процедур.
3. Виконати db/p1/seed.sql: наповнює базу тестовими даними. Кожна вставка перевіряє наявність запису, тому повторний запуск не створює дублікатів.

4.2. Проєкт №2: EastLeaf_CatalogDB (SQL Server)
1. Виконати db/p2/schema.sql: так само перестворює базу; таблиці, зв'язки, обмеження, індекси, представлення vw_ProductCatalog, службові колонки й rowversion.
2. Виконати db/p2/seed.sql: довідникові дані (категорії, товари, характеристики, зображення, гіди заварювання, теги). Повторний запуск безпечний.
Міграції EF Core з'являться в Лаб. 03; поки що ревізії схеми зберігаються як скрипти, історія версій у `docs/migrations.md`.

4.3. Проєкт №3: EastLeaf_ReviewsDB (MongoDB)
Через Compass: відкрити панель >_MONGOSH, вставити вміст файлу, натиснути Enter (за потреби Enter ще раз на порожньому рядку).
1. Виконати db/p3/collections.js: створює колекції reviews, tea_discussions, tea_clubs (якщо їх ще немає).
2. Виконати db/p3/seed.js: наповнює колекції документами. Документ із тим самим _id замінюється, а не додається, тому повторний запуск не створює дублікатів. Після виконання має вивестися reviews: 3, tea_discussions: 1, tea_clubs: 2.

Через термінал (потрібен mongosh), з кореня репозиторію:
mongosh "mongodb://localhost:27017" --file db/p3/collections.js
mongosh "mongodb://localhost:27017" --file db/p3/seed.js

База EastLeaf_ReviewsDB у MongoDB створюється автоматично при першому записі. Контроль схеми через JSON Schema Validation описаний у docs/migrations.md як приклад і до бази не застосовується (крок 7 додатковий).

Перегляд ER-діаграм та генерація в SSMS
Готові діаграми проєкту: У папці `docs/` знаходяться візуальні схеми `ERD_1DB.png` та `ERD_2DB.png`, які демонструють зв'язки між таблицями баз даних.
Як створити діаграму самостійно в SSMS:
  1. Відкрийте SQL Server Management Studio та підключіться до сервера.
  2. У деревовидному меню (Object Explorer) знайдіть потрібну базу даних.
  3. Знайдіть папку Database Diagrams, натисніть правою кнопкою миші та оберіть New Database Diagram.
  4. Виділіть потрібні таблиці та натисніть Add, щоб згенерувати графічну ER-діаграму.



## 2. Лаб. 02: мікросервіс Orders (ADO.NET + Dapper, тришарова архітектура)

### 2.1. Схема проєкту
```text
Platform.slnx
├── Platform.AppHost/             оркестрація Aspire: SQL Server + Orders.Api
└── Services/Orders/
    ├── Orders.Api/               Web/API: контролери, Program.cs, обробка помилок (ProblemDetails), Swagger
    ├── Orders.Bll/               сервіси, DTO, профілі AutoMapper, бізнес-валідація
    ├── Orders.Dal/               репозиторії (ADO.NET + Dapper), Unit of Work
    └── Orders.Domain/            моделі та доменні винятки
```
Залежності йдуть в один бік: **Api -> Bll -> Dal**; `Domain` використовується всіма шарами. Контролер не звертається до репозиторіїв напряму, жоден SQL-запит не виходить за межі `Orders.Dal`.

| Шар | Що містить |
|---|---|
| Orders.Dal | `CustomerRepository` (чистий ADO.NET, без Generic Repository); `OrderRepository`, `ProductRepository`, `PaymentRepository`, `PromotionRepository` (Dapper, спільна база `BaseDapperRepository<T>`); `UnitOfWork` (одне з'єднання й одна транзакція на запит); `SqlErrors` (перетворення винятків БД у доменні) |
| Orders.Bll | `OrderService` (транзакційні операції оформлення та підтвердження замовлення чаю та аксесуарів), `CustomerService`, `ProductService`, `PaymentService`, `PromotionService` |
| Orders.Api | `OrdersController`, `CustomersController`, `ProductsController`, `PaymentsController`, `PromotionsController`; `DomainExceptionHandler` |


## 2.2. Що потрібно для запуску
Для успішного запуску та тестування проєкту необхідне наступне програмне забезпечення:
1. **.NET 9.0 SDK** (або новішої версії) із встановленим робочим навантаженням .NET Aspire.
2. **Microsoft SQL Server** (або LocalDB / SQL Server Express / Docker).
3. **MongoDB** (для сервісу відгуків, опціонально).
4. **Visual Studio 2022** (або VS Code) з підтримкою Docker та .NET Aspire.


## 2.3. Запуск застосунку через .NET Aspire та розгортання
1. **Відкрийте рішення:**
   * Відкрийте файл `Platform.slnx` у Visual Studio.
2. **Налаштування підключення до бази даних:**
   * Рядок підключення (`ConnectionString`) до бази даних конфігурується централізовано в проєкті **`Platform.AppHost`** у файлі `Program.cs`.
   * Для локальної розробки Aspire автоматично піднімає та керує контейнером бази даних.
   * Мікросервіс `orders-api` отримує параметри підключення автоматично через оркестратор (за допомогою методу `.WithReference(db)` в `AppHost` та `builder.Add...ConnectionString("ordersdb")` у самому сервісі).
3. **Запуск оркестратора:**
   * У *Solution Explorer* встановіть проєкт **`Platform.AppHost`** як стартовий (**Set as Startup Project**).
   * Натисніть **F5** (або кнопку **Start**).
4. **Контроль стану через Aspire Dashboard:**
   * Після запуску автоматично відкриється сторінка **Aspire Dashboard**.
   * Перевірте, що всі зареєстровані ресурси (контейнер бази даних, мікросервіс `orders-api`) мають зелений статус **Running**.
5. **Тестування API:**
   * У таблиці ресурсів Aspire Dashboard у рядку `orders-api` натисніть на посилання у колонці **Endpoints**.
   * У браузері відкриється сторінка **Swagger UI**, де можна перевірити працездатність ендпоінтів та взаємодію з базою даних.


## 2.4. Ендпоінти й статуси відповідей
| Метод і шлях | Успіх | Помилки |
|---|---|---|
| `GET /api/customers`, `GET /api/customers/{id}` | 200 | 404 |
| `POST /api/customers` | 201 + Location | 400 (валідація), 409 (дубль Email) |
| `DELETE /api/customers/{id}` | 204 | 404, 409 (наявні активні замовлення) |
| `GET /api/products`, `GET /api/products/{id}` | 200 | 404 |
| `POST /api/orders` | 201 + Location | 400, 404 (клієнт/товар не знайдено), 409 (брак товару) |
| `POST /api/orders/{id}/confirm` | 204 | 404, 409 (недопустимий статус, брак товару) |

Усі помилки повертаються у стандартному форматі **ProblemDetails** завдяки централізованому обробнику `DomainExceptionHandler`.


## 2.5. Приклади запитів (cURL та Postman)
Основні ендпоїнти сервісу замовлень та клієнтів доступні через Swagger UI або безпосередньо через HTTP-запити.

### Отримати інформацію про клієнта за ID
**HTTP Request:** `GET /api/customers/1`

**cURL команда:**
```bash
curl -X 'GET' \
  'https://localhost:7271/api/customers/1' \
  -H 'accept: application/json'
```

**Приклад успішної відповіді (200 OK):**
```json
{
  "customerID": 1,
  "firstName": "Olena",
  "lastName": "Petrenko",
  "email": "olena.petrenko@gmail.com",
  "phoneNumber": "+380501112233",
  "deliveryAddress": "Kyiv, Khreshch st., 1",
  "isActive": true
}
```

### Створити нового клієнта
**HTTP Request:** `POST /api/customers`

**cURL команда:**
```bash
curl -X 'POST' \
  'https://localhost:7271/api/customers' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
    "firstName": "Dmytro",
    "lastName": "Kovalenko",
    "email": "dmytro.kov@gmail.com",
    "phoneNumber": "+380509998877",
    "deliveryAddress": "Lviv, Svobody av., 10",
    "isActive": true
  }'
```

**Приклад успішної відповіді (201 Created):**
Headers: `Location: /api/customers/2`
```json
{
  "customerID": 2,
  "firstName": "Dmytro",
  "lastName": "Kovalenko",
  "email": "dmytro.kov@gmail.com",
  "phoneNumber": "+380509998877",
  "deliveryAddress": "Lviv, Svobody av., 10",
  "isActive": true
}
```


## 3. Лаб. 03: мікросервіс Catalog (EF Core Code First, тришарова архітектура)

Сервіс каталогу товарів: чай, посуд і набори з категоріями, деталями, зображеннями та тегами. Побудований на **EF Core 8 (Code First)**, використовує репозиторії та Unit of Work, DTO з AutoMapper, валідацію, централізовану обробку помилок, Serilog і шаблон Specification.

### 3.1. Схема проєкту
```text
Platform.slnx
├── Platform.AppHost/             оркестрація Aspire: orders-api та catalog-api
└── Services/Catalog/
    ├── Catalog.Api/              Web/API: контролери (Products, Categories, Tags), Program.cs,
    │                             GlobalExceptionHandler (ProblemDetails), Serilog, Swagger
    ├── Catalog.Bll/              сервіси, DTO, MappingProfile (AutoMapper), валідатори (FluentValidation),
    │                             ProductQuery і PagedResult
    ├── Catalog.Dal/              AppDbContext, Configurations (Fluent API), Migrations,
    │                             репозиторії, Unit of Work, Specifications (Ardalis), DbInitializer
    └── Catalog.Domain/           сутності з бізнес-правилами та доменні винятки
```
Залежності йдуть в один бік: **Api -> Bll -> Dal -> Domain**. Контролер не звертається до репозиторіїв напряму, жодного запиту до БД поза `Catalog.Dal` немає.

| Шар | Що містить |
|---|---|
| Catalog.Domain | `Category`, `Product`, `ProductDetail`, `ProductImage`, `Tag`, `ProductTag`; приватні сеттери й бізнес-правила в самих сутностях (`ChangePrice`, `AddStock`, `RemoveStock`); `DomainException`, `ConflictException`, `EntityNotFoundException` |
| Catalog.Dal | `AppDbContext`, конфігурації Fluent API (CHECK-обмеження, індекси, seed довідників), міграції; `GenericRepository<T>`, `ProductRepository`, `CategoryRepository`, `TagRepository`, `ProductTagRepository`; `UnitOfWork`; специфікації `ProductsFilterSpec`, `ProductsPageSpec`, `ProductWithDetailsSpec`; `DbInitializer` |
| Catalog.Bll | `ProductService`, `CategoryService`, `TagService`; DTO; `MappingProfile`; `CreateProductDtoValidator`, `ProductQueryValidator`; `ProductQuery`, `PagedResult<T>` |
| Catalog.Api | `ProductsController`, `CategoriesController`, `TagsController`; `GlobalExceptionHandler`; конфігурація Serilog |


### 3.2. Модель даних
Шість таблиць зі всіма типами зв'язків:

| Зв'язок | Таблиці |
|---|---|
| 1:1 | `Products` - `ProductDetails` |
| 1:N | `Categories` - `Products`, `Products` - `ProductImages` |
| M:N | `Products` - `Tags` через `ProductTags` (із полем `AddedDate`, яке ставить БД) |


### 3.3. Що зроблено в лабі
1. **Code First.** Сутності, Fluent API-конфігурації, міграції `InitialCreate` і `SeedReferenceData`; SQL-скрипт міграції лежить у `Catalog.Dal/Migrations/InitialCreate.sql`.
2. **Репозиторії та Unit of Work.** Один `UnitOfWork` (і один `DbContext`) на HTTP-запит. Три стратегії завантаження пов'язаних даних: Eager (картка товару одним запитом), Explicit (категорія з товарами двома запитами) і M:N через `ProductTag`.
3. **DTO, AutoMapper, API.** Окремі DTO для читання та запису, `MappingProfile`, три контролери. Ліцензійний ключ AutoMapper зберігається в User Secrets.
4. **Seeding.** Довідники (5 категорій, 5 тегів) додаються через `HasData`; тестові товари додає `DbInitializer` (ідемпотентно, у транзакції, у Production не запускається).
5. **Пагінація, фільтрація, сортування.** Виконуються на рівні SQL (`IQueryable`), відповідь містить метадані сторінки.
6. **Валідація.** Прості правила: DataAnnotations; складні: FluentValidation (окремі класи-валідатори). Помилки повертаються як `ValidationProblemDetails` (400).
7. **Обробка помилок.** Один глобальний обробник `GlobalExceptionHandler` (`IExceptionHandler` + `AddProblemDetails`).
8. **Логування.** Serilog зі структурованими логами, `CorrelationId`, `RequestPath`, `UserId`, логами SQL-запитів EF Core.
9. **Specification.** Запити винесені в специфікації (Ardalis.Specification), репозиторій лише застосовує їх.


### 3.4. Що потрібно для запуску
1. **.NET 8 SDK** або новіший (проєкт `Catalog.Api` збирається під `net8.0`).
2. **Microsoft SQL Server** (локальний інстанс, LocalDB, Express або Docker).
3. **Інструмент dotnet-ef** для міграцій: `dotnet tool install --global dotnet-ef --version 8.*`.
4. **Ліцензійний ключ AutoMapper** (Community-ліцензія безкоштовна). У репозиторій ключ **не комітиться**, його задають у User Secrets:
```bash
dotnet user-secrets set "AutoMapper:LicenseKey" "<ваш-ключ>" --project Services/Catalog/Catalog.Api
```
5. **Рядок підключення** `ConnectionStrings:CatalogDb` у `Services/Catalog/Catalog.Api/appsettings.json`. Для локального SQL Server з Windows-автентифікацією це приблизно:
```text
Server=localhost;Database=EastLeaf_CatalogDB;Trusted_Connection=True;TrustServerCertificate=True
```
Підключення можна перевизначити через User Secrets або змінну оточення `ConnectionStrings__CatalogDb`.

> **Важливо.** Скрипт `db/p2/schema.sql` з Лаб. 01 видаляє й перестворює базу `EastLeaf_CatalogDB`, яку тепер створює і веде EF Core. Для Лаб. 03 базу створюють лише міграції EF, а скрипти `db/p2` одночасно з ними не виконують. Якщо скрипт уже запускався, зверху на цю базу міграції не накладуться: спершу видаліть базу.


### 3.5. Міграції EF Core
Усі команди виконуються з кореня репозиторію. `Catalog.Dal` містить міграції, `Catalog.Api` є стартовим проєктом.

**Застосувати міграції до бази** (створить базу, якщо її немає):
```bash
dotnet ef database update --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api
```

**Додати нову міграцію** після зміни моделі:
```bash
dotnet ef migrations add <НазваМіграції> --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api
```

**Згенерувати SQL-скрипт** усіх міграцій:
```bash
dotnet ef migrations script --idempotent --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api -o Services/Catalog/Catalog.Dal/Migrations/InitialCreate.sql
```

Міграції також застосовуються автоматично при старті застосунку (`MigrateAsync`). Команда `dotnet ef database update` не має параметра `--environment`: середовище задається змінною `ASPNETCORE_ENVIRONMENT`.

Історія застосованих міграцій: `SELECT * FROM __EFMigrationsHistory`.


### 3.6. Запуск і перевірка
**Варіант 1: лише Catalog.Api.** Встановіть `Catalog.Api` стартовим проєктом і запустіть (профіль `https`). Swagger відкривається за адресою `https://localhost:7208/swagger`.

**Варіант 2: через Aspire.** Встановіть `Platform.AppHost` стартовим проєктом. У ньому зареєстровано `catalog-api` поруч з `orders-api`; Swagger відкривається за посиланням у колонці **Endpoints** в Aspire Dashboard.

При старті в середовищі Development застосунок застосовує міграції та запускає `DbInitializer`. Повторний запуск не створює дублікатів: сідер перевіряє наявність товарів і завершується, якщо вони є. У Production сідінг вимкнений.


### 3.7. Ендпоінти й статуси відповідей
| Метод і шлях | Успіх | Помилки |
|---|---|---|
| `GET /api/products` (пагінація, фільтри, сортування) | 200 | 400 (невалідні параметри) |
| `GET /api/products/{id}` (картка з деталями, зображеннями й тегами) | 200 | 404 |
| `POST /api/products` | 201 + Location | 400, 404 (категорія не знайдена), 409 (назва вже існує) |
| `PUT /api/products/{id}/price` | 200 | 400, 404 |
| `POST /api/products/{id}/stock/add` | 200 | 400, 404 |
| `POST /api/products/{id}/stock/remove` | 200 | 400, 404, 409 (недостатньо на залишку) |
| `DELETE /api/products/{id}` | 204 | 404 |
| `POST /api/products/{id}/tags/{tagId}` | 204 | 404, 409 (тег уже доданий) |
| `DELETE /api/products/{id}/tags/{tagId}` | 204 | 404 |
| `GET /api/categories`, `GET /api/categories/{id}` | 200 | 404 |
| `POST /api/categories`, `PUT /api/categories/{id}` | 201 / 200 | 400, 404 |
| `GET /api/tags`, `POST /api/tags` | 200 / 201 | 400 |

**Параметри `GET /api/products`:** `page`, `pageSize` (1-100), `search`, `categoryId`, `tagId`, `minPrice`, `maxPrice`, `isAvailable`, `sortBy` (`name` | `price` | `stock`), `sortDir` (`asc` | `desc`).

**Відповідь списку:** `items`, `totalCount`, `page`, `pageSize`, `totalPages`, `hasNext`, `hasPrevious`; ті самі підсумки дублюються в заголовках `X-Total-Count` і `X-Total-Pages`.

**Відповідність винятків і HTTP-статусів** (централізований `GlobalExceptionHandler`):

| Виняток | Статус |
|---|---|
| `EntityNotFoundException` | 404 |
| `ValidationException` (FluentValidation) | 400 |
| `ConflictException` | 409 |
| `DomainException` (порушення бізнес-правила) | 409 |
| будь-який інший | 500 (у `detail` лише загальне повідомлення; повні деталі тільки в Development) |

Помилки валідації вхідних даних повертаються як `ValidationProblemDetails` (400) з переліком помилок по полях. Усі відповіді з помилками мають формат ProblemDetails (`application/problem+json`) з полями `status`, `title`, `detail`, `instance` і `traceId`.


### 3.8. Приклади запитів
**Список товарів: друга сторінка, сортування за ціною**

`GET /api/products?page=2&pageSize=5&sortBy=price&sortDir=desc`
```bash
curl -X 'GET' \
  'https://localhost:7208/api/products?page=2&pageSize=5&sortBy=price&sortDir=desc' \
  -H 'accept: application/json'
```
Відповідь 200 містить `items` (до 5 товарів), `totalCount` (кількість усіх товарів за фільтром, а не лише на сторінці) і метадані сторінки; заголовки `X-Total-Count`, `X-Total-Pages`.

**Створити товар**

`POST /api/products`
```bash
curl -X 'POST' \
  'https://localhost:7208/api/products' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
    "categoryId": 1,
    "productName": "Da Hong Pao 100g",
    "price": 350
  }'
```
Відповідь 201 Created містить заголовок `Location: /api/products/{id}` і створений товар.

**Помилка валідації (400):** порожня назва товару
```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "ProductName": [ "'Product Name' must not be empty." ]
  }
}
```

**Товар не знайдено (404):** `GET /api/products/99999`
```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Product 99999 was not found.",
  "instance": "/api/products/99999",
  "traceId": "..."
}
```

### 3.9. Логування (Serilog)
- Налаштування: секція `Serilog` в `appsettings.json` (рівні `Default` і `Override`) і код у `Program.cs`. Змінні оточення також підтримуються, наприклад `Serilog__MinimumLevel__Default=Warning`.
- Sink: консоль, шаблон виводить усі властивості (`{Properties:j}`).
- У кожному рядку запиту є `CorrelationId`, `UserId` (поки немає автентифікації, значення `anonymous`) і `RequestPath`.
- Виняток логується **один раз**, у `GlobalExceptionHandler`: 404 і 409 як `Warning`, 500 як `Error`. Стандартний лог `ExceptionHandlerMiddleware` вимкнений.
- SQL-команди EF Core логуються на рівні `Information`. У логах видно різницю між стратегіями: Eager Loading (`GET /api/products/{id}`) виконує **один** SQL-запит, Explicit Loading (категорія з товарами) **два**, а список товарів два: `SELECT COUNT(*)` і `SELECT ... OFFSET ... FETCH`.


## 4. Лаб. 04: мікросервіс Reviews (MongoDB, Clean Architecture, CQRS + MediatR)

Сервіс відгуків: відгуки на товари з вбудованими коментарями. На відміну від тришарових сервісів Лаб. 02-03, тут застосовано **Clean Architecture**: чотири шари, де головний шар (Domain) не залежить ні від API, ні від бази даних.

### 4.1. Схема проєкту
```text
Platform.slnx
├── Platform.AppHost/             оркестрація Aspire: orders-api, catalog-api, reviews-api
└── Services/Reviews/
    ├── Reviews.Domain/           сутності, Value Objects, доменні винятки, інтерфейс репозиторію
    ├── Reviews.Application/      команди, запити, обробники, валідатори, behaviors, DTO
    ├── Reviews.Infrastructure/   доступ до MongoDB: контекст, BSON-мапінг, репозиторій, запити, індекси, сидінг
    ├── Reviews.WebApi/           контролери, Program.cs, AutoMapper, обробка помилок, Swagger, health check
    └── Reviews.Tests/            unit- та інтеграційні тести
```
- **Domain** містить лише доменну логіку й не має залежності навіть від драйвера MongoDB (використовується лише тип `ObjectId` з пакета `MongoDB.Bson`).
- **Application** залежить лише від Domain. Він оголошує інтерфейси (`IReviewQueries`), а реалізація цих інтерфейсів живе в Infrastructure.
- **Infrastructure** працює з MongoDB і реалізує інтерфейси внутрішніх шарів.
- **WebApi** містить контролери й підключає всі шари під час старту.

| Шар | Що містить |
|---|---|
| Reviews.Domain | `BaseEntity` (Id, CreatedAt, UpdatedAt, Version), `ValueObject`; сутності `Review` і `Comment`; Value Objects `Author` і `Rating`; `DomainException`, `NotFoundException`, `ConflictException`; `IReviewRepository` |
| Reviews.Application | команди `CreateReview`, `UpdateReview`, `DeleteReview`, `AddComment`, `RemoveComment`; запити `GetReviewById`, `GetReviewsList`; `ValidationBehavior`, `LoggingBehavior`, `PerformanceBehavior`; FluentValidation-валідатори; `ReviewDto`, `PagedResult<T>`; `IReviewQueries` |
| Reviews.Infrastructure | `MongoDbContext`, `MongoMappings` (BsonClassMap), `RatingSerializer`, `ReviewRepository`, `ReviewQueries`; `IndexCreationService`, `ReviewSeeder`, `DataSeedingService` |
| Reviews.WebApi | `ReviewsController`, `GlobalExceptionHandler`, `ReviewProfile` і `ReviewRequestProfile` (AutoMapper), `MongoHealthCheck`, Serilog, Swagger |


### 4.2. Як проходить запит (CQRS + MediatR)
Кожна операція API це окремий об'єкт: **команда** (змінює дані) або **запит** (лише читає). Контролер нічого не робить сам, а лише передає цей об'єкт у MediatR.

```text
Контролер ─► MediatR ─► Logging ─► Validation ─► Performance ─► Обробник ─► MongoDB
```
- **LoggingBehavior** записує в лог початок і результат запиту.
- **ValidationBehavior** запускає всі валідатори FluentValidation. Якщо є помилки, вони збираються разом, і **обробник не викликається**.
- **PerformanceBehavior** пише попередження, якщо обробник працює довше за 500 мс.

**Чому два способи доступу до БД.** Команди працюють через `IReviewRepository` (повна сутність, бізнес-правила). Запити читання працюють через `IReviewQueries`: MongoDB сама формує готовий `ReviewDto` (проєкція), рахує кількість коментарів і виконує пагінацію (`skip/limit`), а не завантажує зайве в пам'ять.


### 4.3. Що зроблено в лабі
1. **Структура Clean Architecture.** Чотири проєкти з коректними посиланнями; Domain не має посилань на інші проєкти.
2. **Доменний шар.** Сутності з приватними сеттерами та методами зміни стану (`Review.Update`, `Review.AddComment`); порушення правил кидає `DomainException`. Value Objects `Author` і `Rating` незмінні, порівнюються за значенням і перевіряються в конструкторі.
3. **CQRS через MediatR.** Команди й запити з окремими обробниками; обробники не звертаються до `IMongoCollection` напряму.
4. **Pipeline Behaviors і валідатори.** Валідація виконується до обробника; перевіряється коректність `ObjectId`; є асинхронне правило із запитом до MongoDB («користувач уже залишав відгук на цей товар»).
5. **MongoDB.** Контекст налаштовується через `IOptions<MongoDbOptions>`. BSON-мапінг описаний окремо через `BsonClassMap`, тому доменні класи не містять атрибутів MongoDB. Назви полів у camelCase, рейтинг зберігається числом, а приватний список `_comments` записується як масив `comments`.
6. **Оптимістична конкурентність.** Кожен документ має `version`. Оновлення застосовується лише до документа з очікуваною версією, інакше 409. Клієнт отримує версію в заголовку `ETag` при читанні та передає її в `If-Match` при оновленні.
7. **Індекси** створюються автоматично при старті (див. 4.4).
8. **Seeding.** `ReviewSeeder` додає початкові відгуки, перевіряючи, чи вони вже є; повторний запуск не створює дублікатів.
9. **Централізована обробка помилок** (див. 4.7): доменні, валідаційні та MongoDB-винятки перетворюються на ProblemDetails з правильним статусом.
10. **Конфігурація.** Рядок підключення й назва бази в секції `MongoDb` з окремими значеннями для Development / Staging / Production; параметри пулу й таймаутів задаються в рядку підключення (`maxPoolSize`, `serverSelectionTimeoutMS`); health check `/health` перевіряє зв'язок із MongoDB.


### 4.4. Модель документа та індекси
Відгук зберігається одним документом: автор і коментарі вбудовані (вони читаються разом із відгуком), а на товар є лише посилання `productId` із денормалізованою назвою `productName`.
```json
{
  "_id": "66f0c2000000000000000001",
  "productId": 4,
  "productName": "Da Hong Pao Oolong",
  "author": { "userId": "1", "displayName": "Olena" },
  "rating": 5,
  "text": "Deep mineral taste, holds up to six infusions.",
  "comments": [
    { "_id": "66f0c5000000000000000001",
      "author": { "userId": "2", "displayName": "Ivan" },
      "text": "Agreed, a really good oolong.",
      "createdAt": "2026-10-06T09:00:00Z" }
  ],
  "version": 0,
  "createdAt": "2026-10-05T10:30:00Z"
}
```
Колекція має гнучку схему: у частини відгуків є додаткові поля (`teaware`, `pros`, `cons`, `discussionId`). Сервіс їх не втрачає при оновленні, бо змінює лише потрібні поля документа.

| Індекс | Навіщо |
|---|---|
| `{ productId: 1, createdAt: -1 }` | «останні відгуки про товар» і сортування в списку |
| text-індекс по `text` | повнотекстовий пошук по тексту відгуків |
| `{ productId: 1, "author.userId": 1 }`, unique | один відгук від автора на товар |

Перевірка в Compass (mongosh): `db.reviews.getIndexes()`. Повторний запуск сервісу індекси не дублює.


### 4.5. Що потрібно для запуску
1. **.NET 8 SDK** або новіший.
2. **MongoDB**, запущена локально на `localhost:27017` (Compass або mongosh для перегляду даних). Для цього сервісу MongoDB обов'язкова.
3. **Ліцензійний ключ MediatR та AutoMapper** (Community-ліцензія безкоштовна, один ключ на обидва пакети). У репозиторій ключ **не комітиться**, його задають у User Secrets:
```bash
dotnet user-secrets set "LuckyPenny:LicenseKey" "<ваш-ключ>" --project Services/Reviews/Reviews.WebApi
```
4. **Конфігурація MongoDB** у `Services/Reviews/Reviews.WebApi/appsettings.json`:
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017/?maxPoolSize=50&serverSelectionTimeoutMS=5000",
    "DatabaseName": "EastLeaf_ReviewsDB"
  }
}
```
Для інших середовищ значення перевизначаються у `appsettings.Staging.json` і `appsettings.Production.json`, а також змінною середовища `MongoDb__ConnectionString`.

Початкові дані сервіс додає сам при першому запуску. Скрипти `db/p3/collections.js` і `db/p3/seed.js` з Лаб. 01 теж можна виконати (див. розділ 1.4): вони створюють ті самі дані й безпечні для повторного запуску.


### 4.6. Запуск і перевірка
**Варіант 1: лише Reviews.WebApi.** Встановіть `Reviews.WebApi` стартовим проєктом і запустіть (профіль `https`). Відкриється Swagger UI за адресою `/swagger`. Стан сервісу й підключення до MongoDB показує `/health`.

**Варіант 2: через Aspire.** Встановіть `Platform.AppHost` стартовим проєктом. У ньому зареєстровано `reviews-api` поруч з `orders-api` і `catalog-api`; Swagger відкривається за посиланням у колонці **Endpoints** в Aspire Dashboard.

При старті сервіс спочатку створює індекси, потім виконує сидінг. Якщо MongoDB недоступна, сервіс запускається й пише помилку в лог, а запити повертають 503.

**Тести.** Запустіть **Test → Run All Tests** у Visual Studio. Unit-тести (Domain, Application) працюють без бази. Інтеграційні тести (Infrastructure, позначені `Category=Integration`) потребують запущеної MongoDB; кожен тест використовує власну тимчасову базу й видаляє її після себе.


### 4.7. Ендпоінти й статуси відповідей
| Метод і шлях | Успіх | Помилки |
|---|---|---|
| `GET /api/reviews` (`productId`, `page`, `pageSize`) | 200 | 400 (невалідні параметри) |
| `GET /api/reviews/{id}` | 200 + `ETag` | 400 (невалідний Id), 404 |
| `POST /api/reviews` | 201 + Location | 400 (валідація, повторний відгук), 409 (дубль за унікальним індексом) |
| `PUT /api/reviews/{id}` (заголовок `If-Match`) | 204 | 400, 404, 409 (конфлікт версій) |
| `DELETE /api/reviews/{id}` | 204 | 400, 404 |
| `POST /api/reviews/{id}/comments` | 201 | 400, 404 |
| `DELETE /api/reviews/{id}/comments/{commentId}` | 204 | 400, 404 |
| `GET /health` | 200 | 503 |

**Відповідність винятків і HTTP-статусів** (централізований `GlobalExceptionHandler`):

| Виняток | Статус |
|---|---|
| `NotFoundException` | 404 |
| `ValidationException` (FluentValidation) | 400 |
| `DomainException` | 400 |
| `ConflictException` (у т. ч. конфлікт версій) | 409 |
| `MongoWriteException` (DuplicateKey) | 409 (інші помилки запису: 400) |
| `MongoConnectionException`, `TimeoutException` | 503 |
| будь-який інший | 500 (клієнту лише загальне повідомлення, деталі в лозі) |

Усі помилки повертаються у форматі **ProblemDetails**. Помилки валідації збираються разом і повертаються списком по полях.


### 4.8. Приклади запитів
**Створити відгук**

`POST /api/reviews`
```bash
curl -X 'POST' \
  'https://localhost:7000/api/reviews' \
  -H 'Content-Type: application/json' \
  -d '{
    "productId": 2,
    "productName": "Darjeeling First Flush",
    "authorId": "10",
    "authorName": "Taras",
    "rating": 5,
    "text": "Light and floral."
  }'
```
Відповідь **201 Created**: заголовок `Location: /api/reviews/{id}` з рядковим `ObjectId`, а в тілі той самий `id`.

**Прочитати відгук і оновити його (оптимістична конкурентність)**

1. `GET /api/reviews/{id}` повертає відгук і заголовок `ETag: "0"` (це `version` документа).
2. `PUT /api/reviews/{id}` із заголовком `If-Match: "0"` і тілом `{ "rating": 4, "text": "Changed my mind" }` повертає **204**, версія стає 1.
3. Другий `PUT` з тим самим `If-Match: "0"` повертає **409 Conflict**, бо документ уже змінений іншим запитом:
```json
{
  "title": "Conflict",
  "status": 409,
  "detail": "Review 66f0... was modified (expected version 0, current 1)",
  "traceId": "..."
}
```

**Помилка валідації (400):** `rating` = 10 і порожній текст
```json
{
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Rating": [ "'Rating' must be between 1 and 5. You entered 10." ],
    "Text": [ "'Text' must not be empty." ]
  }
}
```
Обробник у цьому випадку не викликається, у логах немає запису `Handled CreateReviewCommand`.

**Повторний відгук того самого автора на той самий товар (400):**
```json
{
  "title": "Validation failed",
  "status": 400,
  "errors": { "AuthorId": [ "You have already reviewed this product" ] }
}
```

**Відгук не знайдено (404):** `GET /api/reviews/66f0c9000000000000000009`
```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Review 66f0c9000000000000000009 was not found",
  "traceId": "..."
}
```
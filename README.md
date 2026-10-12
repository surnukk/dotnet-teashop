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
Приклад успішної відповіді (200 OK):
JSON
{
  "customerID": 1,
  "firstName": "Olena",
  "lastName": "Petrenko",
  "email": "olena.petrenko@gmail.com",
  "phoneNumber": "+380501112233",
  "deliveryAddress": "Kyiv, Khreshch st., 1",
  "isActive": true
}
Створити нового клієнта
HTTP Request: POST /api/customers
cURL команда:
Bash
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

Приклад успішної відповіді (201 Created):
Headers: Location: /api/customers/2
JSON
{
  "customerID": 2,
  "firstName": "Dmytro",
  "lastName": "Kovalenko",
  "email": "dmytro.kov@gmail.com",
  "phoneNumber": "+380509998877",
  "deliveryAddress": "Lviv, Svobody av., 10",
  "isActive": true
}
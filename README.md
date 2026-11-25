# Green Auto

ASP.NET Core 10 приложение автосервиса на MVC и Razor Pages.

## Структура проекта

- `Green/Controllers` — MVC-обработчики аккаунтов, заказов, каталога и кабинетов.
- `Green/Pages/ServiceRequests` — Razor Pages для заявок на обслуживание.
- `Green/Models` и `Green/ViewModels` — сущности и модели ввода/отображения.
- `Green/Data` — контекст EF Core и начальная настройка БД/ролей.
- `Green/Infrastructure` — общие вспомогательные правила и типы.
- `Green/Views` — MVC-представления и общие Razor-layouts.
- `Green/wwwroot` — общие CSS и JavaScript.

## База данных

### PostgreSQL и pgAdmin4

Приложение использует **PostgreSQL** в качестве базы данных. Для управления базой данных рекомендуется использовать **pgAdmin4** — веб-интерфейс для администрирования PostgreSQL.

#### Установка PostgreSQL

1. Скачайте инсталлятор с https://www.postgresql.org/download/
2. Запустите установку и запомните пароль для пользователя `postgres`
3. По умолчанию PostgreSQL запускается на порту **5432**
4. Создайте базу данных для приложения:
   ```sql
   CREATE DATABASE green_auto;
   ```

#### Установка и использование pgAdmin4

**pgAdmin4** — это веб-интерфейс для управления PostgreSQL, позволяющий:
- Визуально управлять структурой базы данных
- Выполнять SQL-запросы
- Просматривать и редактировать данные таблиц
- Создавать резервные копии
- Мониторить производительность базы

##### Способ 1: Прямая установка

1. Скачайте pgAdmin4 с https://www.pgadmin.org/download/
2. Установите приложение на локальный компьютер
3. После установки pgAdmin4 откроется в браузере на адресе `http://localhost:80` или `http://localhost:5050`

##### Способ 2: Docker (рекомендуется)

Если у вас установлен Docker, запустите pgAdmin4 командой:
```bash
docker run -p 5050:80 ^
  -e PGADMIN_DEFAULT_EMAIL=admin@example.com ^
  -e PGADMIN_DEFAULT_PASSWORD=admin ^
  dpage/pgadmin4
```

Откройте в браузере `http://localhost:5050` и войдите с логином `admin@example.com` и паролем `admin`.

##### Подключение к PostgreSQL в pgAdmin4

1. Откройте pgAdmin4 в браузере
2. В левой панели щелкните на "Servers"
3. Щелкните правой кнопкой мыши на "Servers" → "Create" → "Server..."
4. На вкладке "General" введите:
   - **Name**: Green Auto (или другое имя сервера)
5. На вкладке "Connection" заполните:
   - **Host name/address**: `localhost`
   - **Port**: `5432`
   - **Username**: `postgres`
   - **Password**: пароль, введённый при установке PostgreSQL
6. Нажмите "Save"

Теперь вы сможете видеть все базы данных на сервере PostgreSQL, включая **`GreenGarageDb`**. Щелкните на этой базе данных, чтобы управлять таблицами, данными и выполнять SQL-запросы.

## Конфигурация

Перед запуском задайте строку подключения `ConnectionStrings:DefaultConnection` и общий пароль начальных учётных записей:

- `InitialAdmin:Password` — общий пароль для начальных учётных записей `admin@green.com`, `master@green.com` и `client@green.com`.

Для разработки используйте .NET Secret Manager (`dotnet user-secrets`) для проекта `Green/Green.csproj`. В окружении развертывания передавайте параметры через защищённое хранилище секретов или переменные окружения `ConnectionStrings__DefaultConnection`, `InitialAdmin__Email` и `InitialAdmin__Password`. Не добавляйте значения паролей и строки подключения в репозиторий.

Пароль должен содержать не менее 8 символов, заглавную букву, цифру и специальный символ. Приложение применяет миграции и создаёт/синхронизирует начальные учётные записи с ролями администратора (`Admin`), мастера (`Employee`) и клиента (`Client`). Для существующих seed-учётных записей также устанавливается общий пароль из секрета. Не используйте общий пароль seed-учётных записей в рабочей среде.

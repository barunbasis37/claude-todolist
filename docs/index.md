# TodoList

A simple ASP.NET Core Razor Pages todo list app: add tasks, mark them complete, and delete them.

## Tech Stack

- **.NET 10** (`net10.0`), ASP.NET Core Web App (Razor Pages)
- **EF Core 10.0.9** with SQLite (`Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`)
- **SQLitePCLRaw.bundle_e_sqlite3 3.0.3** (native SQLite provider)
- Front-end: Bootstrap, jQuery, jQuery Validation, jQuery Validation Unobtrusive (vendored under `wwwroot/lib/`)

## Where to go next

- [Getting Started](getting-started.md) — prerequisites, running the app locally
- [User Guide](user-guide.md) — how to add, complete, and delete tasks in the app
- [Architecture](architecture.md) — project structure, hosting model, key files
- [Data Model](data-model.md) — the `TodoItem` entity
- [Pages & Routes](pages-and-routes.md) — routes, handlers, purpose of each page
- [Extending the App](extending.md) — how to add a new entity + page following the existing pattern

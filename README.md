<!-- document-project: last synced 2026-07-09 -->

<!-- document-project:section:header:start -->
# TodoList

A simple ASP.NET Core Razor Pages todo list app: add tasks, mark them complete, and delete them.
<!-- document-project:section:header:end -->

<!-- document-project:section:tech-stack:start -->
## Tech Stack

- **.NET 10** (`net10.0`), ASP.NET Core Web App (Razor Pages)
- **EF Core 10.0.9** with SQLite (`Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`)
- **SQLitePCLRaw.bundle_e_sqlite3 3.0.3** (native SQLite provider)
- Front-end: Bootstrap, jQuery, jQuery Validation, jQuery Validation Unobtrusive (vendored under `wwwroot/lib/`)
<!-- document-project:section:tech-stack:end -->

<!-- document-project:section:prerequisites:start -->
## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
<!-- document-project:section:prerequisites:end -->

<!-- document-project:section:getting-started:start -->
## Getting Started

```bash
dotnet restore
dotnet run
```

The app starts on:
- HTTP: `http://localhost:5232`
- HTTPS: `https://localhost:7028` (also serves HTTP on `5232`)

No database setup is required — on startup, `Program.cs` calls
`db.Database.EnsureCreated()`, which creates the SQLite file `todo.db` in the
project root automatically if it doesn't already exist. There are no EF Core
migrations in this project; schema changes require deleting `todo.db` and
letting it regenerate (see [CLAUDE.md](CLAUDE.md) for details).
<!-- document-project:section:getting-started:end -->

<!-- document-project:section:project-structure:start -->
## Project Structure

```
TodoList/
├── Program.cs              # App startup: services, middleware, EnsureCreated()
├── Data/
│   └── TodoContext.cs      # EF Core DbContext
├── Models/
│   └── TodoItem.cs         # Todo entity
├── Pages/
│   ├── Index.cshtml(.cs)   # Main todo list page (add/toggle/delete)
│   ├── Privacy.cshtml(.cs) # Static placeholder page
│   ├── Error.cshtml(.cs)   # Error page
│   └── Shared/             # _Layout, validation scripts partial
├── Properties/
│   └── launchSettings.json # Run profiles (http/https ports)
├── wwwroot/                # Static assets (css/js, vendored Bootstrap/jQuery)
└── TodoList.csproj
```
<!-- document-project:section:project-structure:end -->

<!-- document-project:section:data-model:start -->
## Data Model

### `TodoItem` (table backing `TodoContext.TodoItems`)

| Field | Type | Constraints |
|---|---|---|
| `Id` | `int` | Primary key |
| `Title` | `string` | `[Required]`, `[StringLength(200)]` |
| `IsComplete` | `bool` | Defaults to `false` |
| `CreatedAt` | `DateTime` | Defaults to `DateTime.Now` at creation |
<!-- document-project:section:data-model:end -->

<!-- document-project:section:pages-and-routes:start -->
## Pages & Routes

| Route | Page | Handlers | Purpose |
|---|---|---|---|
| `/` | `Pages/Index.cshtml` | `OnGetAsync`, `OnPostAddAsync`, `OnPostToggleAsync(id)`, `OnPostDeleteAsync(id)` | List todos (incomplete first, newest first), add a todo, toggle complete, delete a todo |
| `/Privacy` | `Pages/Privacy.cshtml` | `OnGet` | Static placeholder page |
| `/Error` | `Pages/Error.cshtml` | `OnGet` | Standard error page, shows `RequestId` |
<!-- document-project:section:pages-and-routes:end -->

<!-- document-project:section:configuration:start -->
## Configuration

The SQLite connection string is read from configuration key `ConnectionStrings:TodoContext`
(see `appsettings.json`); if not set, it falls back to `Data Source=todo.db` in
`Program.cs`. No connection string is currently defined in `appsettings.json`, so
the app uses the fallback.
<!-- document-project:section:configuration:end -->

<!-- document-project:end-of-generated-content -->

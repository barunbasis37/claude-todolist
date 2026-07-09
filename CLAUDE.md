<!-- document-project: last synced 2026-07-09 -->

<!-- document-project:section:overview:start -->
## Overview

ASP.NET Core Razor Pages todo list app (.NET 10, EF Core + SQLite). See
[README.md](README.md) for full setup, data model, and routes detail.
<!-- document-project:section:overview:end -->

<!-- document-project:section:commands:start -->
## Commands

```bash
dotnet build
dotnet run
```

Run profiles (`Properties/launchSettings.json`): `http` → `http://localhost:5232`,
`https` → `https://localhost:7028` (+ `http://localhost:5232`). No test project
exists in this repo yet, so there is no `dotnet test` target.
<!-- document-project:section:commands:end -->

<!-- document-project:section:architecture:start -->
## Architecture Notes

- Minimal hosting model in `Program.cs` — no `Startup.cs`.
- Razor Pages file-based routing: route = file path under `Pages/` (e.g.
  `Pages/Index.cshtml` → `/`).
- Single `DbContext` (`TodoContext`) registered via `AddDbContext` and injected
  into `IndexModel` via constructor DI.
- **Schema is created via `Database.EnsureCreated()`, not EF Core migrations.**
  There is no `Migrations/` folder. If you change the `TodoItem` model, the
  existing `todo.db` file will NOT be automatically updated — delete `todo.db`
  locally to let it regenerate, or introduce a proper migration (`dotnet ef
  migrations add ...`) if you need production-safe schema evolution. Do not
  assume migrations are in use.
- POST handlers (`Add`/`Toggle`/`Delete`) use standard Razor Pages
  `asp-page-handler` forms with anti-forgery tokens (default Razor Pages
  behavior, not custom code).
<!-- document-project:section:architecture:end -->

<!-- document-project:section:key-files:start -->
## Key Files

| File | Why it matters |
|---|---|
| `Program.cs` | Composition root: registers Razor Pages + `TodoContext`, calls `EnsureCreated()`, configures middleware pipeline |
| `Data/TodoContext.cs` | The only `DbContext`; one `DbSet<TodoItem>` |
| `Models/TodoItem.cs` | The only entity/domain model |
| `Pages/Index.cshtml.cs` | All todo CRUD logic (add/toggle/delete) lives here |
| `Pages/Index.cshtml` | The only substantive UI in the app |
<!-- document-project:section:key-files:end -->

<!-- document-project:section:extending-the-app:start -->
## Extending the App

To add a new entity + page, following the existing pattern:
1. Add a POCO under `Models/`.
2. Add a `DbSet<T>` property to `Data/TodoContext.cs`.
3. Add `Pages/<Name>.cshtml` + `Pages/<Name>.cshtml.cs` with `OnGet`/`OnPost*`
   handlers (inject `TodoContext` via constructor, same as `IndexModel`).
4. Link the new page from `Pages/Shared/_Layout.cshtml` nav if it needs to be
   reachable from the header.
5. Remember: no migrations are in use — `EnsureCreated()` only creates tables
   that don't exist yet in `todo.db`; it will not alter existing tables for
   changed models.
<!-- document-project:section:extending-the-app:end -->

<!-- document-project:section:gotchas:start -->
## Gotchas

- No test project exists in this repo.
- No `.git` repository is present as of the last scan — do not assume git
  commands (`git log`, `git diff`) are available.
- No CI configuration found.
- No EF Core migrations — see Architecture Notes above before changing
  `Models/TodoItem.cs`.
<!-- document-project:section:gotchas:end -->

<!-- document-project:end-of-generated-content -->

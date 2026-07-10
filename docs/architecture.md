# Architecture Notes

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

## Key Files

| File | Why it matters |
|---|---|
| `Program.cs` | Composition root: registers Razor Pages + `TodoContext`, calls `EnsureCreated()`, configures middleware pipeline |
| `Data/TodoContext.cs` | The only `DbContext`; one `DbSet<TodoItem>` |
| `Models/TodoItem.cs` | The only entity/domain model |
| `Pages/Index.cshtml.cs` | All todo CRUD logic (add/toggle/delete) lives here |
| `Pages/Index.cshtml` | The only substantive UI in the app |

## Gotchas

- No test project exists in this repo, so there is no `dotnet test` target.
- No CI configuration for the app itself (docs deploy via GitHub Actions —
  see the repo's `.github/workflows/`).
- No EF Core migrations — see above before changing `Models/TodoItem.cs`.

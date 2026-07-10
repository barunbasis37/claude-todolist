# Extending the App

To add a new entity + page, following the existing pattern:

1. Add a POCO under `Models/`.
2. Add a `DbSet<T>` property to `Data/TodoContext.cs`.
3. Add `Pages/<Name>.cshtml` + `Pages/<Name>.cshtml.cs` with `OnGet`/`OnPost*`
   handlers (inject `TodoContext` via constructor, same as `IndexModel`).
4. Link the new page from `Pages/Shared/_Layout.cshtml` nav if it needs to be
   reachable from the header.
5. Remember: no migrations are in use — `EnsureCreated()` only creates tables
   that don't exist yet in `todo.db`; it will not alter existing tables for
   changed models. Delete `todo.db` locally to let it regenerate, or
   introduce a proper migration (`dotnet ef migrations add ...`) if you need
   production-safe schema evolution.

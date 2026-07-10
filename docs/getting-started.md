# Getting Started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later

## Run the app

```bash
dotnet restore
dotnet run
```

The app starts on:

- HTTP: `http://localhost:5232`
- HTTPS: `https://localhost:7028` (also serves HTTP on `5232`)

Run profiles are defined in `Properties/launchSettings.json`.

## Database

No database setup is required — on startup, `Program.cs` calls
`db.Database.EnsureCreated()`, which creates the SQLite file `todo.db` in the
project root automatically if it doesn't already exist.

There are **no EF Core migrations** in this project; schema changes require
deleting `todo.db` and letting it regenerate. See
[Extending the App](extending.md) for details.

## Configuration

The SQLite connection string is read from configuration key
`ConnectionStrings:TodoContext` (see `appsettings.json`); if not set, it falls
back to `Data Source=todo.db` in `Program.cs`. No connection string is
currently defined in `appsettings.json`, so the app uses the fallback.

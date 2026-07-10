# TodoList

A simple ASP.NET Core Razor Pages todo list app: add tasks, mark them complete, and delete them.

## Quick Start

```bash
dotnet restore
dotnet run
```

The app starts on `http://localhost:5232` (and `https://localhost:7028`). No
database setup is required — `todo.db` (SQLite) is created automatically on
first run.

## Documentation

Full docs — architecture, data model, routes, and how to extend the app —
live in [`docs/`](docs/) and are published with [MkDocs](https://www.mkdocs.org/):

- [docs/index.md](docs/index.md)
- [docs/getting-started.md](docs/getting-started.md)
- [docs/architecture.md](docs/architecture.md)
- [docs/data-model.md](docs/data-model.md)
- [docs/pages-and-routes.md](docs/pages-and-routes.md)
- [docs/extending.md](docs/extending.md)

To preview the docs site locally:

```bash
pip install -r requirements-docs.txt
mkdocs serve
```

See also [CLAUDE.md](CLAUDE.md) for guidance aimed at AI coding assistants
working in this repo.

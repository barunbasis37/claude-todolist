# Pages & Routes

| Route | Page | Handlers | Purpose |
|---|---|---|---|
| `/` | `Pages/Index.cshtml` | `OnGetAsync`, `OnPostAddAsync`, `OnPostToggleAsync(id)`, `OnPostDeleteAsync(id)` | List todos (incomplete first, newest first), add a todo, toggle complete, delete a todo |
| `/Privacy` | `Pages/Privacy.cshtml` | `OnGet` | Static placeholder page |
| `/Error` | `Pages/Error.cshtml` | `OnGet` | Standard error page, shows `RequestId` |

Razor Pages uses file-based routing, so the route is derived directly from the
file path under `Pages/`. All todo CRUD logic lives in `Pages/Index.cshtml.cs`.

POST handlers (`Add`/`Toggle`/`Delete`) use standard Razor Pages
`asp-page-handler` forms with anti-forgery tokens — this is default Razor
Pages behavior, not custom code.

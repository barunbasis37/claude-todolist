# Data Model

## `TodoItem`

Table backing `TodoContext.TodoItems`.

| Field | Type | Constraints |
|---|---|---|
| `Id` | `int` | Primary key |
| `Title` | `string` | `[Required]`, `[StringLength(200)]` |
| `IsComplete` | `bool` | Defaults to `false` |
| `CreatedAt` | `DateTime` | Defaults to `DateTime.Now` at creation |

This is the only entity/domain model in the app (`Models/TodoItem.cs`), and
`TodoContext` exposes a single `DbSet<TodoItem>` for it.

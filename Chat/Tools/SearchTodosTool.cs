using System.Text.Json;
using Anthropic.Models.Messages;
using Microsoft.EntityFrameworkCore;
using TodoList.Data;
using TodoList.Models;

namespace TodoList.Chat.Tools;

/// <summary>
/// Searches the caller's own todo list by keyword, or retrieves a single
/// item by 1-based index / a first-last shortcut. This is the template for
/// any future tool: implement <see cref="IChatTool"/>, register it in
/// Program.cs, and <see cref="ChatToolCatalog"/> handles the rest
/// (inclusion in the request, deferred loading once the catalog grows,
/// logging).
/// </summary>
public class SearchTodosTool : IChatTool
{
    private readonly TodoContext _context;

    public SearchTodosTool(TodoContext context)
    {
        _context = context;
    }

    public string Name => "search_todos";

    public string Description =>
        "Search the user's todo list by keyword, or retrieve a single todo by its 1-based position " +
        "(optionally within the keyword-filtered results) — including shortcuts for the first or last item. " +
        "Returns matching items with their completion status and creation date. Call this before answering " +
        "any question about specific tasks on the user's list — do not answer from assumption.";

    public InputSchema InputSchema => new()
    {
        Properties = new Dictionary<string, JsonElement>
        {
            ["query"] = JsonSerializer.SerializeToElement(new
            {
                type = "string",
                description = "Keyword or phrase to search for in todo titles. Leave empty to consider the whole list.",
            }),
            ["includeCompleted"] = JsonSerializer.SerializeToElement(new
            {
                type = "boolean",
                description = "Whether to include already-completed todos. Defaults to false.",
            }),
            ["index"] = JsonSerializer.SerializeToElement(new
            {
                type = "integer",
                description = "1-based position of a specific todo to retrieve, e.g. 3 for 'the 3rd one'. " +
                    "Applied after the query filter, if any.",
            }),
            ["position"] = JsonSerializer.SerializeToElement(new
            {
                type = "string",
                @enum = new[] { "first", "last" },
                description = "Shortcut for the first or last todo, e.g. for 'what's my first todo?' or " +
                    "'what did I add most recently?'. Applied after the query filter, if any.",
            }),
        },
    };

    public async Task<string> ExecuteAsync(IReadOnlyDictionary<string, JsonElement> input)
    {
        var query = input.TryGetValue("query", out var queryElement) ? queryElement.GetString() ?? string.Empty : string.Empty;
        var includeCompleted = input.TryGetValue("includeCompleted", out var includeElement)
            && includeElement.ValueKind == JsonValueKind.True;
        int? index = input.TryGetValue("index", out var indexElement) && indexElement.ValueKind == JsonValueKind.Number
            ? indexElement.GetInt32()
            : null;
        var position = input.TryGetValue("position", out var positionElement) && positionElement.ValueKind == JsonValueKind.String
            ? positionElement.GetString()
            : null;

        var candidates = await _context.TodoItems
            .Where(t => includeCompleted || !t.IsComplete)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        var matches = string.IsNullOrEmpty(query)
            ? candidates
            : candidates.Where(t => t.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        if (position == "first" || position == "last")
        {
            var item = position == "first" ? matches.FirstOrDefault() : matches.LastOrDefault();
            return item is null ? "No matching todo items found." : FormatTodo(item);
        }

        if (index is not null)
        {
            return index < 1 || index > matches.Count
                ? $"No todo found at index {index}. There are {matches.Count} matching todo item(s)."
                : FormatTodo(matches[index.Value - 1]);
        }

        return matches.Count == 0 ? "No matching todo items found." : string.Join("\n", matches.Select(FormatTodo));
    }

    private static string FormatTodo(TodoItem item) =>
        $"- [{(item.IsComplete ? "x" : " ")}] {item.Title} (added {item.CreatedAt:MMM d, yyyy})";
}

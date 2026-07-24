using Microsoft.EntityFrameworkCore;
using TodoList.Data;
using TodoList.Models;

namespace TodoList.Services;

/// <summary>
/// The one piece of business logic behind "search_todos", shared by every
/// surface that exposes it: the in-app chat tool (<see cref="TodoList.Chat.Tools.SearchTodosTool"/>)
/// and the MCP tool (<see cref="TodoList.Mcp.TodoMcpTools"/>). Each surface
/// only adapts its own name/description/schema conventions around this.
/// </summary>
public class TodoSearchService
{
    private readonly TodoContext _context;

    public TodoSearchService(TodoContext context)
    {
        _context = context;
    }

    public async Task<string> SearchAsync(string query, bool includeCompleted, int? index, string? position)
    {
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

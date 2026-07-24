using Microsoft.EntityFrameworkCore;
using TodoList.Data;
using TodoList.Models;

namespace TodoList.Services;

/// <summary>
/// The write-side counterpart to <see cref="TodoSearchService"/>: add/toggle/delete,
/// currently only exposed over MCP (<see cref="TodoList.Mcp.TodoMcpTools"/>) but kept
/// separate from search so an in-app chat tool could reuse it later the same way
/// <see cref="TodoList.Chat.Tools.SearchTodosTool"/> reuses <see cref="TodoSearchService"/>.
/// </summary>
public class TodoMutationService
{
    private readonly TodoContext _context;

    public TodoMutationService(TodoContext context)
    {
        _context = context;
    }

    public async Task<string> AddAsync(string? title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return "Cannot add a todo with an empty title.";
        }

        if (trimmed.Length > 200)
        {
            return "Todo title is too long (max 200 characters).";
        }

        var item = new TodoItem { Title = trimmed };
        _context.TodoItems.Add(item);
        await _context.SaveChangesAsync();

        return $"Added todo #{item.Id}: \"{item.Title}\".";
    }

    public async Task<string> ToggleAsync(int id)
    {
        var item = await _context.TodoItems.FindAsync(id);
        if (item is null)
        {
            return $"No todo found with id {id}.";
        }

        item.IsComplete = !item.IsComplete;
        await _context.SaveChangesAsync();

        return $"Todo #{item.Id} \"{item.Title}\" is now {(item.IsComplete ? "complete" : "incomplete")}.";
    }

    public async Task<string> DeleteAsync(int id, string? confirmTitle)
    {
        var item = await _context.TodoItems.FindAsync(id);
        if (item is null)
        {
            return $"No todo found with id {id}.";
        }

        // Deletion is destructive and irreversible, and the id alone comes from
        // whatever text search_todos last returned to the caller — which could
        // be an LLM steered by injected instructions hidden in a todo's own
        // title. Requiring the caller to echo the title back confirms they
        // (or the model) actually looked at the right item rather than just
        // replaying a number.
        if (!string.Equals(confirmTitle?.Trim(), item.Title, StringComparison.OrdinalIgnoreCase))
        {
            return $"Refused: confirmTitle does not match todo #{id}'s title (\"{item.Title}\"). " +
                "Call search_todos to confirm the exact title, then retry with confirmTitle set to it.";
        }

        _context.TodoItems.Remove(item);
        await _context.SaveChangesAsync();

        return $"Deleted todo #{item.Id} \"{item.Title}\".";
    }
}

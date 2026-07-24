using System.ComponentModel;
using ModelContextProtocol.Server;
using TodoList.Services;

namespace TodoList.Mcp;

/// <summary>
/// The MCP-facing surface of the todo tools. Thin by design — all logic lives
/// in <see cref="TodoSearchService"/>, shared with the in-app chat tool
/// (<see cref="TodoList.Chat.Tools.SearchTodosTool"/>). Add a new tool here
/// (or a new class alongside it, then <c>.WithTools&lt;T&gt;()</c> in
/// Program.cs) to expose more of the app over MCP.
/// </summary>
[McpServerToolType]
public sealed class TodoMcpTools
{
    private readonly TodoSearchService _search;

    public TodoMcpTools(TodoSearchService search)
    {
        _search = search;
    }

    [McpServerTool(Name = "search_todos")]
    [Description(
        "Search the user's todo list by keyword, or retrieve a single todo by its 1-based position " +
        "(optionally within the keyword-filtered results) — including shortcuts for the first or last item. " +
        "Returns matching items with their completion status and creation date.")]
    public async Task<string> SearchTodos(
        [Description("Keyword or phrase to search for in todo titles. Leave empty to consider the whole list.")]
        string query = "",
        [Description("Whether to include already-completed todos. Defaults to false.")]
        bool includeCompleted = false,
        [Description("1-based position of a specific todo to retrieve, e.g. 3 for 'the 3rd one'. Applied after the query filter, if any.")]
        int? index = null,
        [Description("Shortcut for the first or last todo: \"first\" or \"last\". Applied after the query filter, if any.")]
        string? position = null)
    {
        return await _search.SearchAsync(query, includeCompleted, index, position);
    }
}

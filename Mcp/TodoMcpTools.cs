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
    private readonly TodoMutationService _mutation;

    public TodoMcpTools(TodoSearchService search, TodoMutationService mutation)
    {
        _search = search;
        _mutation = mutation;
    }

    [McpServerTool(Name = "search_todos")]
    [Description(
        "Search the user's todo list by keyword, or retrieve a single todo by its 1-based position " +
        "(optionally within the keyword-filtered results) — including shortcuts for the first or last item. " +
        "Returns matching items with their id, completion status, and creation date. The id is needed for " +
        "toggle_todo/delete_todo.")]
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

    [McpServerTool(Name = "add_todo")]
    [Description("Add a new todo item to the user's list.")]
    public async Task<string> AddTodo(
        [Description("The title/text of the new todo, e.g. 'Buy milk'.")]
        string title)
    {
        return await _mutation.AddAsync(title);
    }

    [McpServerTool(Name = "toggle_todo")]
    [Description(
        "Flip a todo's completion status (incomplete -> complete, or complete -> incomplete). " +
        "Use search_todos first to find the todo's id if you don't already have it.")]
    public async Task<string> ToggleTodo(
        [Description("The id of the todo to toggle, as returned by search_todos.")]
        int id)
    {
        return await _mutation.ToggleAsync(id);
    }

    [McpServerTool(Name = "delete_todo")]
    [Description(
        "Permanently delete a todo from the user's list. Use search_todos first to find the todo's id and " +
        "exact title — both are required, and the delete is refused if confirmTitle doesn't match, so you " +
        "can't delete an item you haven't actually looked up.")]
    public async Task<string> DeleteTodo(
        [Description("The id of the todo to delete, as returned by search_todos.")]
        int id,
        [Description("The todo's exact current title, as returned by search_todos, confirming this is the intended item.")]
        string confirmTitle)
    {
        return await _mutation.DeleteAsync(id, confirmTitle);
    }
}

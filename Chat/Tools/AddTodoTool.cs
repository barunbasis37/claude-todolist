using System.Text.Json;
using Anthropic.Models.Messages;
using TodoList.Services;

namespace TodoList.Chat.Tools;

/// <summary>
/// Adds a new todo item on the user's behalf. Mirrors <see cref="SearchTodosTool"/>'s
/// shape: implements <see cref="IChatTool"/>, register in Program.cs, and
/// <see cref="ChatToolCatalog"/> handles the rest. The actual write logic lives in
/// <see cref="TodoMutationService"/>, shared with the MCP-exposed version of this
/// same tool (<see cref="TodoList.Mcp.TodoMcpTools"/>).
/// </summary>
public class AddTodoTool : IChatTool
{
    private readonly TodoMutationService _mutation;

    public AddTodoTool(TodoMutationService mutation)
    {
        _mutation = mutation;
    }

    public string Name => "add_todo";

    public string Description =>
        "Add a new todo item to the user's list. Use this whenever the user asks to add, create, or note down " +
        "a task — do not just acknowledge it in text without actually adding it.";

    public InputSchema InputSchema => new()
    {
        Properties = new Dictionary<string, JsonElement>
        {
            ["title"] = JsonSerializer.SerializeToElement(new
            {
                type = "string",
                description = "The title/text of the new todo, e.g. 'Buy milk'.",
            }),
        },
        Required = new List<string> { "title" },
    };

    public async Task<string> ExecuteAsync(IReadOnlyDictionary<string, JsonElement> input)
    {
        var title = input.TryGetValue("title", out var titleElement) ? titleElement.GetString() : null;
        return await _mutation.AddAsync(title);
    }
}

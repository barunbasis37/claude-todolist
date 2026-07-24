using System.Text.Json;
using Anthropic.Models.Messages;

namespace TodoList.Chat;

/// <summary>
/// One function the chat AI can call. Implement this and register it in
/// Program.cs (<c>builder.Services.AddScoped&lt;IChatTool, YourTool&gt;()</c>)
/// to add a new tool — <see cref="ChatToolCatalog"/> picks it up automatically,
/// including deciding whether it should be deferred once the catalog grows
/// past the tool-search threshold. No other code needs to change.
/// </summary>
public interface IChatTool
{
    string Name { get; }

    string Description { get; }

    InputSchema InputSchema { get; }

    Task<string> ExecuteAsync(IReadOnlyDictionary<string, JsonElement> input);
}

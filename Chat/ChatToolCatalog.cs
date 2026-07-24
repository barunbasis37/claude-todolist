using System.Text.Json;
using Anthropic.Models.Messages;

namespace TodoList.Chat;

/// <summary>
/// Central registry for every <see cref="IChatTool"/> the chat AI can call.
/// Handles two things that shouldn't live inside individual tools:
///
/// 1. Deciding whether to send the full tool list as-is, or switch to
///    Anthropic's server-side tool search (deferred loading) once the
///    catalog grows large enough that sending every schema on every request
///    would waste tokens and hurt tool-selection accuracy. The threshold and
///    strategy come straight from Anthropic's own guidance: tool search pays
///    off at 10+ tools, and 3-5 of the most-used tools should stay loaded so
///    common calls skip the search round-trip. This activates automatically
///    as tools are added — nothing here needs to change to go from 1 tool to
///    200.
///
/// 2. Uniform logging for every function call (console + a per-tool JSON
///    dump), so a new tool gets the same observability as every other one
///    without its author having to remember to add it.
/// </summary>
public class ChatToolCatalog
{
    // Anthropic's own guidance: below 10 tools, sending full schemas every
    // request is simpler and just as accurate; tool search starts paying off
    // at or above that.
    private const int ToolSearchThreshold = 10;

    // Keep this many tools (in registration order) always loaded, so the
    // most common calls don't need a search round-trip first. Anthropic
    // recommends 3-5.
    private const int NonDeferredCount = 5;

    private static readonly string LogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ChatLogs");

    private readonly IReadOnlyList<IChatTool> _tools;
    private readonly Dictionary<string, IChatTool> _byName;

    public ChatToolCatalog(IEnumerable<IChatTool> tools)
    {
        _tools = tools.ToList();
        _byName = _tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public bool UsesToolSearch => _tools.Count >= ToolSearchThreshold;

    /// <summary>
    /// Builds the <c>tools</c> array for a Messages API request: the search
    /// tool plus every registered tool, with <c>defer_loading</c> applied
    /// once the catalog is large enough to need it.
    /// </summary>
    public List<ToolUnion> BuildToolUnions()
    {
        var unions = new List<ToolUnion>();

        if (UsesToolSearch)
        {
            // BM25 (natural-language queries) fits this catalog better than
            // regex — tool names/descriptions here read as plain English,
            // not code-like identifiers.
            unions.Add(new ToolUnion(new ToolSearchToolBm25_20251119
            {
                Type = ToolSearchToolBm25_20251119Type.ToolSearchToolBm25_20251119,
            }));
        }

        for (var i = 0; i < _tools.Count; i++)
        {
            var tool = _tools[i];
            unions.Add(new ToolUnion(new Tool
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = tool.InputSchema,
                DeferLoading = UsesToolSearch && i >= NonDeferredCount,
            }));
        }

        return unions;
    }

    /// <summary>
    /// Runs a client-side tool call by name, with console logging and a
    /// per-tool request/response JSON dump (same overwrite-per-day behavior
    /// as the rest of ChatLogs). Never called for <c>server_tool_use</c>
    /// blocks (e.g. the tool search tool itself) — those resolve entirely
    /// server-side and must never receive a tool_result.
    /// </summary>
    public async Task<string> ExecuteAsync(string toolName, string toolUseId, IReadOnlyDictionary<string, JsonElement> input)
    {
        var inputJson = JsonSerializer.Serialize(input);
        Console.WriteLine($"[FunctionCalling] tool='{toolName}' id='{toolUseId}' input={inputJson}");

        string result;
        if (_byName.TryGetValue(toolName, out var tool))
        {
            result = await tool.ExecuteAsync(input);
        }
        else
        {
            result = $"Unknown tool '{toolName}'.";
            Console.WriteLine($"[FunctionCalling] tool='{toolName}' is not recognized — returning fallback result.");
        }

        Console.WriteLine($"[ToolResult] tool='{toolName}' -> {result}");

        await WriteToolCallLogAsync(toolName, inputJson, result);

        return result;
    }

    private static async Task WriteToolCallLogAsync(string toolName, string inputJson, string result)
    {
        Directory.CreateDirectory(LogDirectory);

        var safeName = new string(toolName.Where(c => char.IsLetterOrDigit(c) || c is '_' or '-').ToArray());
        var fileName = $"tool-{safeName}-{DateTime.Now:yyyy-MM-dd}.json";
        var filePath = Path.Combine(LogDirectory, fileName);

        var log = new
        {
            timestamp = DateTime.Now.ToString("o"),
            tool = toolName,
            request = JsonSerializer.Deserialize<JsonElement>(inputJson),
            response = new { text = result },
        };

        var json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(filePath, json);
    }
}

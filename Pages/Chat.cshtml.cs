using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TodoList.Chat;

namespace TodoList.Pages;

public class ChatModel : PageModel
{
    // Dumps the most recent request/response pair for the day to disk as
    // formatted JSON. Filename is per-day, so later requests on the same day
    // overwrite it rather than accumulating a log.
    private static readonly string LogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ChatLogs");

    private const string SystemPrompt = "You are a friendly brainstorming partner inside a personal todo list app. " +
        "Help the user think through, prioritize, break down, or plan their tasks. " +
        "Keep replies conversational and concise. " +
        "Use the search_todos tool whenever the user asks about specific tasks, wants to check whether something " +
        "is on their list, references their todos by name or keyword, asks for a specific todo by number, or " +
        "asks for their first or last todo — never guess or invent todo items. " +
        "Use the add_todo tool whenever the user asks to add, create, or note down a task — actually call the " +
        "tool rather than just acknowledging it in text. " +
        "You may use light markdown (bold with **text**, bullet lists with '- ') when it makes a list of tasks easier to read.";

    // Common greetings/small-talk answered from a static FAQ instead of calling
    // the model — cheap, instant, and safe since these never depend on the
    // client's own todo list (which differs per client).
    private static readonly Dictionary<string, string> StaticReplies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hi"] = "Hey there! What's on your mind — need help prioritizing, breaking down a task, or just thinking out loud?",
        ["hello"] = "Hello! What would you like to brainstorm today?",
        ["hey"] = "Hey! What's on your mind?",
        ["yo"] = "Hey! What's on your mind?",
        ["good morning"] = "Good morning! What would you like to work through today?",
        ["good afternoon"] = "Good afternoon! What would you like to work through today?",
        ["good evening"] = "Good evening! What would you like to work through today?",
        ["thanks"] = "You're welcome! Let me know if you want to keep brainstorming.",
        ["thank you"] = "You're welcome! Let me know if you want to keep brainstorming.",
        ["bye"] = "Take care! Come back anytime you want to plan things out.",
        ["goodbye"] = "Take care! Come back anytime you want to plan things out.",
        ["who are you"] = "I'm a brainstorming assistant built into this todo app — I can help you prioritize, break tasks down, or just think out loud about your list.",
        ["what can you do"] = "I can search your todo list, help you prioritize tasks, break big todos into smaller steps, or just talk through what's on your plate. What would you like to start with?",
        ["help"] = "I can search your todo list, help you prioritize tasks, break big todos into smaller steps, or just talk through what's on your plate. What would you like to start with?",
    };

    private readonly AnthropicClient _anthropicClient;
    private readonly ChatToolCatalog _tools;

    public ChatModel(AnthropicClient anthropicClient, ChatToolCatalog tools)
    {
        _anthropicClient = anthropicClient;
        _tools = tools;
    }

    public void OnGet()
    {
    }

    public class ChatTurn
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequest
    {
        public List<ChatTurn> Messages { get; set; } = new();
    }

    public async Task<IActionResult> OnPostSendAsync([FromBody] ChatRequest request)
    {
        if (request.Messages.Count == 0)
        {
            return BadRequest();
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var lastMessage = request.Messages[^1].Content;
        var staticReply = GetStaticReply(lastMessage);
        if (staticReply is not null)
        {
            await WriteSseEventAsync("delta", staticReply);
            await WriteSseEventAsync("done", string.Empty);
            await WriteRequestResponseLogAsync(request, staticReply, error: null);
            return new EmptyResult();
        }

        var messages = request.Messages
            .Select(m => new MessageParam
            {
                Role = m.Role == "assistant" ? Role.Assistant : Role.User,
                Content = m.Content,
            })
            .ToList();

        var finalText = new StringBuilder();
        string? error = null;

        try
        {
            var toolUnions = _tools.BuildToolUnions();
            Console.WriteLine($"[Tools] catalog size={toolUnions.Count} usesToolSearch={_tools.UsesToolSearch}");

            // Manual tool-use loop: Claude may call one or more tools —
            // including the tool-search tool once the catalog is large
            // enough to use it — before producing its final answer. Capped
            // so a misbehaving tool-call chain can't loop forever.
            for (var turn = 0; turn < 5; turn++)
            {
                var response = await _anthropicClient.Messages.Create(new MessageCreateParams
                {
                    Model = Model.ClaudeHaiku4_5,
                    MaxTokens = 1024,
                    System = SystemPrompt,
                    Tools = toolUnions,
                    Messages = messages,
                });

                foreach (var block in response.Content)
                {
                    if (block.TryPickText(out var text))
                    {
                        finalText.Append(text.Text);
                    }
                }

                if (response.StopReason != "tool_use")
                {
                    break;
                }

                var assistantContent = new List<ContentBlockParam>();
                var toolResults = new List<ContentBlockParam>();

                foreach (var block in response.Content)
                {
                    if (block.TryPickText(out var text))
                    {
                        assistantContent.Add(new TextBlockParam { Text = text.Text });
                    }
                    else if (block.TryPickServerToolUse(out var serverToolUse))
                    {
                        // The tool-search tool itself: resolved entirely
                        // server-side. Echo it back unchanged — never return
                        // a tool_result for a server_tool_use (srvtoolu_...) block.
                        assistantContent.Add(new ServerToolUseBlockParam
                        {
                            ID = serverToolUse.ID,
                            Name = serverToolUse.Name.ToString(),
                            Input = serverToolUse.Input,
                        });
                        Console.WriteLine($"[ToolSearch] turn={turn} query={JsonSerializer.Serialize(serverToolUse.Input)}");
                    }
                    else if (block.TryPickToolSearchToolResult(out var searchResult))
                    {
                        // The discovered-tool references: also echoed back
                        // unchanged so Claude can reuse them without re-searching.
                        assistantContent.Add(new ToolSearchToolResultBlockParam
                        {
                            ToolUseID = searchResult.ToolUseID,
                            Content = ToToolSearchResultParamContent(searchResult.Content),
                        });
                        Console.WriteLine($"[ToolSearch] turn={turn} result={JsonSerializer.Serialize(searchResult.Content)}");
                    }
                    else if (block.TryPickToolUse(out var toolUse))
                    {
                        assistantContent.Add(new ToolUseBlockParam
                        {
                            ID = toolUse.ID,
                            Name = toolUse.Name,
                            Input = toolUse.Input,
                        });

                        var result = await _tools.ExecuteAsync(toolUse.Name, toolUse.ID, toolUse.Input);

                        toolResults.Add(new ToolResultBlockParam
                        {
                            ToolUseID = toolUse.ID,
                            Content = result,
                        });
                    }
                }

                messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantContent });
                if (toolResults.Count > 0)
                {
                    messages.Add(new MessageParam { Role = Role.User, Content = toolResults });
                }
            }

            await WriteSseEventAsync("delta", finalText.ToString());
            await WriteSseEventAsync("done", string.Empty);
        }
        catch (AnthropicUnauthorizedException)
        {
            error = "The server's ANTHROPIC_API_KEY is missing or invalid.";
            await WriteSseEventAsync("error", error);
        }
        catch (AnthropicRateLimitException)
        {
            error = "Rate limited by the Claude API. Please try again shortly.";
            await WriteSseEventAsync("error", error);
        }
        catch (AnthropicApiException ex)
        {
            error = "Chat request failed: " + ex.Message;
            await WriteSseEventAsync("error", error);
        }

        await WriteRequestResponseLogAsync(request, finalText.ToString(), error);

        return new EmptyResult();
    }

    // Response and request block types differ even for a pure echo-back, per
    // the SDK's general pattern (see ToolUseBlock -> ToolUseBlockParam) — no
    // .ToParam() helper exists, so reconstruct the union manually.
    private static ToolSearchToolResultBlockParamContent ToToolSearchResultParamContent(ToolSearchToolResultBlockContent content)
    {
        if (content.TryPickToolSearchToolSearchResultBlock(out var searchBlock))
        {
            return new ToolSearchToolSearchResultBlockParam
            {
                ToolReferences = searchBlock.ToolReferences
                    .Select(r => new ToolReferenceBlockParam { ToolName = r.ToolName })
                    .ToList(),
            };
        }

        if (content.TryPickToolSearchToolResultError(out var errorBlock))
        {
            return new ToolSearchToolResultErrorParam
            {
                ErrorCode = errorBlock.ErrorCode.ToString(),
                ErrorMessage = errorBlock.ErrorMessage,
            };
        }

        throw new InvalidOperationException("Unrecognized tool_search_tool_result content variant.");
    }

    private static string? GetStaticReply(string message)
    {
        var normalized = message.Trim().TrimEnd('!', '?', '.', ' ');
        return StaticReplies.TryGetValue(normalized, out var reply) ? reply : null;
    }

    private async Task WriteSseEventAsync(string eventName, string text)
    {
        var payload = JsonSerializer.Serialize(new { text });
        await Response.WriteAsync($"event: {eventName}\ndata: {payload}\n\n");
        await Response.Body.FlushAsync();
    }

    private static async Task WriteRequestResponseLogAsync(ChatRequest request, string responseText, string? error)
    {
        Directory.CreateDirectory(LogDirectory);

        var fileName = $"request-response-{DateTime.Now:yyyy-MM-dd}.json";
        var filePath = Path.Combine(LogDirectory, fileName);

        var log = new
        {
            timestamp = DateTime.Now.ToString("o"),
            request = new { messages = request.Messages },
            response = new { text = responseText, error },
        };

        var json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(filePath, json);
    }
}

using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoList.Data;

namespace TodoList.Pages;

public class ChatModel : PageModel
{
    private readonly TodoContext _context;
    private readonly AnthropicClient _anthropicClient;

    public ChatModel(TodoContext context, AnthropicClient anthropicClient)
    {
        _context = context;
        _anthropicClient = anthropicClient;
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

        var openTodos = await _context.TodoItems
            .Where(t => !t.IsComplete)
            .OrderBy(t => t.CreatedAt)
            .Select(t => t.Title)
            .ToListAsync();

        var systemPrompt = "You are a friendly brainstorming partner inside a personal todo list app. " +
            "Help the user think through, prioritize, break down, or plan their tasks. " +
            "Keep replies conversational and concise.";

        if (openTodos.Count > 0)
        {
            systemPrompt += "\n\nThe user's current open todo items:\n" + string.Join("\n", openTodos.Select(t => "- " + t));
        }

        try
        {
            var response = await _anthropicClient.Messages.Create(new MessageCreateParams
            {
                Model = Model.ClaudeOpus4_8,
                MaxTokens = 1024,
                Thinking = new ThinkingConfigAdaptive(),
                System = systemPrompt,
                Messages = request.Messages
                    .Select(m => new MessageParam
                    {
                        Role = m.Role == "assistant" ? Role.Assistant : Role.User,
                        Content = m.Content,
                    })
                    .ToList(),
            });

            var reply = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
            return new JsonResult(new { reply });
        }
        catch (AnthropicUnauthorizedException)
        {
            return StatusCode(500, new { error = "The server's ANTHROPIC_API_KEY is missing or invalid." });
        }
        catch (AnthropicRateLimitException)
        {
            return StatusCode(429, new { error = "Rate limited by the Claude API. Please try again shortly." });
        }
        catch (AnthropicApiException ex)
        {
            return StatusCode(502, new { error = "Chat request failed: " + ex.Message });
        }
    }
}

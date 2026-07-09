using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoList.Data;
using TodoList.Models;

namespace TodoList.Pages;

public class IndexModel : PageModel
{
    private readonly TodoContext _context;

    public IndexModel(TodoContext context)
    {
        _context = context;
    }

    public IList<TodoItem> TodoItems { get; set; } = new List<TodoItem>();

    [BindProperty]
    public string? NewTitle { get; set; }

    public int RemainingCount { get; set; }

    public async Task OnGetAsync()
    {
        await LoadTodosAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewTitle))
        {
            _context.TodoItems.Add(new TodoItem { Title = NewTitle.Trim() });
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var item = await _context.TodoItems.FindAsync(id);
        if (item is not null)
        {
            item.IsComplete = !item.IsComplete;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var item = await _context.TodoItems.FindAsync(id);
        if (item is not null)
        {
            _context.TodoItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task LoadTodosAsync()
    {
        TodoItems = await _context.TodoItems
            .OrderBy(t => t.IsComplete)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        RemainingCount = TodoItems.Count(t => !t.IsComplete);
    }
}

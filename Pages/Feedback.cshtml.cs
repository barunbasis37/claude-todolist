using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoList.Data;
using TodoList.Models;

namespace TodoList.Pages;

public class FeedbackModel : PageModel
{
    private readonly TodoContext _context;

    public FeedbackModel(TodoContext context)
    {
        _context = context;
    }

    public IList<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public int? EditingId { get; set; }

    [BindProperty]
    public string? Name { get; set; }

    [BindProperty]
    public string? Message { get; set; }

    [BindProperty]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; } = 5;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public async Task OnGetAsync(int? editId)
    {
        if (editId.HasValue)
        {
            var item = await _context.Feedbacks.FindAsync(editId.Value);
            if (item is not null)
            {
                EditingId = item.Id;
                Name = item.Name;
                Message = item.Message;
                Rating = item.Rating;
            }
        }

        await LoadFeedbacksAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (ModelState.IsValid && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Message))
        {
            _context.Feedbacks.Add(new Feedback
            {
                Name = Name.Trim(),
                Message = Message.Trim(),
                Rating = Rating
            });
            await _context.SaveChangesAsync();
        }
        else
        {
            TempData["FeedbackError"] = "Please enter your name and a message.";
        }

        return RedirectToReturnUrl();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id)
    {
        var item = await _context.Feedbacks.FindAsync(id);
        if (item is not null && ModelState.IsValid && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Message))
        {
            item.Name = Name.Trim();
            item.Message = Message.Trim();
            item.Rating = Rating;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var item = await _context.Feedbacks.FindAsync(id);
        if (item is not null)
        {
            _context.Feedbacks.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task LoadFeedbacksAsync()
    {
        Feedbacks = await _context.Feedbacks
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    private IActionResult RedirectToReturnUrl()
    {
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage();
    }
}

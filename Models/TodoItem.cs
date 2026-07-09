using System.ComponentModel.DataAnnotations;

namespace TodoList.Models;

public class TodoItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter a task.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public bool IsComplete { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

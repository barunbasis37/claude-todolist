using System.ComponentModel.DataAnnotations;

namespace TodoList.Models;

public class Feedback
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your feedback.")]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; } = 5;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

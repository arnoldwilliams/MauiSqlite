namespace MauiSqlite.Core.Models;

/// <summary>
/// A single task in the to-do list.
/// </summary>
public class TodoItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public bool IsCompleted { get; set; }

    public TodoPriority Priority { get; set; } = TodoPriority.Normal;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DueDateUtc { get; set; }
}

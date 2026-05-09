using System.ComponentModel.DataAnnotations;
using FocusSpace.Domain.Enums;

namespace FocusSpace.Application.DTOs
{
    public class TaskDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateTaskDto
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(300, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 300 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    }

    public class UpdateTaskDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(300, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 300 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    }
}
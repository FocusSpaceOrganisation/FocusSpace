using FocusSpace.Domain.Enums;

namespace FocusSpace.Domain.Entities
{
    public class Task
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Task priority — stored as string in the database.</summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
namespace FocusSpace.Application.DTOs
{
    public class SessionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? TaskId { get; set; }
        public string? TaskTitle { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan PlannedDuration { get; set; }
        public TimeSpan? ActualDuration { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSessionDto
    {
        public int UserId { get; set; }
        public int? TaskId { get; set; }
        public TimeSpan PlannedDuration { get; set; }
    }

    public class UpdateSessionDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? EndTime { get; set; }
        public string? ActualDuration { get; set; }
    }

    public class FocusRecommendationDto
    {
        public TimeSpan RecommendedDuration { get; set; }
        public string BestFocusPeriod { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
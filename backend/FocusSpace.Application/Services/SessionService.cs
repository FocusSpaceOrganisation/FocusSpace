using FocusSpace.Application.DTOs;
using FocusSpace.Application.Interfaces;
using FocusSpace.Domain.Entities;
using FocusSpace.Domain.Enums;

namespace FocusSpace.Application.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;

    public SessionService(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<int> StartSessionAsync(CreateSessionDto dto)
    {
        var session = new Session
        {
            UserId = dto.UserId,
            TaskId = dto.TaskId,
            StartTime = DateTime.UtcNow,
            PlannedDuration = dto.PlannedDuration,
            Status = SessionStatus.Ongoing,
            CreatedAt = DateTime.UtcNow
        };

        await _sessionRepository.AddAsync(session);
        await _sessionRepository.SaveChangesAsync();

        return session.Id;
    }

    public async System.Threading.Tasks.Task CompleteSessionAsync(UpdateSessionDto dto)
    {
        var session = await _sessionRepository.GetByIdAsync(dto.Id);
        if (session is null) throw new KeyNotFoundException($"Session {dto.Id} not found");

        session.EndTime = dto.EndTime;
        session.ActualDuration = dto.ActualDuration != null
            ? TimeSpan.Parse(dto.ActualDuration)
            : null;
        session.Status = Enum.Parse<SessionStatus>(dto.Status);

        await _sessionRepository.SaveChangesAsync();
    }

    public async System.Threading.Tasks.Task PauseSessionAsync(int sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session is null) throw new KeyNotFoundException($"Session {sessionId} not found");
        if (session.Status != SessionStatus.Ongoing)
            throw new InvalidOperationException("Can only pause an ongoing session");

        session.Status = SessionStatus.Paused;
        await _sessionRepository.SaveChangesAsync();
    }

    public async System.Threading.Tasks.Task ResumeSessionAsync(int sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session is null) throw new KeyNotFoundException($"Session {sessionId} not found");
        if (session.Status != SessionStatus.Paused)
            throw new InvalidOperationException("Can only resume a paused session");

        session.Status = SessionStatus.Ongoing;
        await _sessionRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<SessionDto>> GetSessionsByUserIdAsync(int userId)
    {
        var sessions = await _sessionRepository.GetByUserIdAsync(userId);
        return sessions.Select(s => new SessionDto
        {
            Id = s.Id,
            UserId = s.UserId,
            TaskId = s.TaskId,
            TaskTitle = s.Task?.Title,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            PlannedDuration = s.PlannedDuration,
            ActualDuration = s.ActualDuration,
            Status = s.Status.ToString(),
            CreatedAt = s.CreatedAt
        });
    }

    public async Task<SessionDto?> GetSessionByIdAsync(int sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session is null) return null;

        return new SessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            TaskId = session.TaskId,
            TaskTitle = session.Task?.Title,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            PlannedDuration = session.PlannedDuration,
            ActualDuration = session.ActualDuration,
            Status = session.Status.ToString(),
            CreatedAt = session.CreatedAt
        };
    }

    public async Task<FocusRecommendationDto> GetFocusRecommendationAsync(int userId)
    {
        var sessions = (await _sessionRepository.GetByUserIdAsync(userId))
            .Where(s => s.ActualDuration.HasValue && s.EndTime.HasValue)
            .ToList();

        if (!sessions.Any())
        {
            return new FocusRecommendationDto
            {
                RecommendedDuration = TimeSpan.FromMinutes(25),
                BestFocusPeriod = "Any time",
                Details = "Not enough completed sessions yet to create a recommendation."
            };
        }

        var averageMinutes = sessions.Average(s => s.ActualDuration!.Value.TotalMinutes);
        var recommendedDuration = TimeSpan.FromMinutes(Math.Max(1, Math.Round(averageMinutes)));

        var bestHourGroup = sessions
            .GroupBy(s => s.StartTime.Hour)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .First();

        var startHour = bestHourGroup.Key;
        var endHour = (startHour + 1) % 24;
        var bestFocusPeriod = $"{startHour:00}:00 - {endHour:00}:00";

        return new FocusRecommendationDto
        {
            RecommendedDuration = recommendedDuration,
            BestFocusPeriod = bestFocusPeriod,
            Details = $"Based on {sessions.Count} completed session(s)."
        };
    }
}

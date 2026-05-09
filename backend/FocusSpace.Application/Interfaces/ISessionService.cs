using FocusSpace.Application.DTOs;

namespace FocusSpace.Application.Interfaces;

public interface ISessionService
{
    Task<int> StartSessionAsync(CreateSessionDto dto);
    Task CompleteSessionAsync(UpdateSessionDto dto);
    Task PauseSessionAsync(int sessionId);
    Task ResumeSessionAsync(int sessionId);
    Task<IEnumerable<SessionDto>> GetSessionsByUserIdAsync(int userId);
    Task<SessionDto?> GetSessionByIdAsync(int sessionId);
    Task<FocusRecommendationDto> GetFocusRecommendationAsync(int userId);
}

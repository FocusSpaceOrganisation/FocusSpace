using FocusSpace.Application.DTOs;

namespace FocusSpace.Application.Interfaces;

public interface ISessionService
{
    Task<int> StartSessionAsync(CreateSessionDto dto);
    Task CompleteSessionAsync(UpdateSessionDto dto);
    Task PauseSessionAsync(int sessionId);
    Task ResumeSessionAsync(int sessionId);
    Task<SessionDto?> GetSessionByIdAsync(int sessionId);
}

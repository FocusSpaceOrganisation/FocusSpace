using FocusSpace.Application.DTOs;
using FocusSpace.Application.Interfaces;
using FocusSpace.Domain.Entities;
using FocusSpace.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FocusSpace.Api.Controllers;

[Authorize(Roles = "User,Admin")]
public class SessionController : Controller
{
    private readonly ISessionService _sessionService;
    private readonly UserManager<User> _userManager;
    private readonly ITaskService _taskService;

    public SessionController(ISessionService sessionService, UserManager<User> userManager, ITaskService taskService)
    {
        _sessionService = sessionService;
        _userManager = userManager;
        _taskService = taskService;
    }

    private async Task<int> GetCurrentUserIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Id ?? throw new InvalidOperationException("Authenticated user not found.");
    }

    private static bool IsActiveStatus(string status)
        => string.Equals(status, nameof(SessionStatus.Ongoing), StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, nameof(SessionStatus.Paused), StringComparison.OrdinalIgnoreCase);

    private async Task<SessionDto?> GetActiveSessionDtoAsync(int userId)
    {
        var sessions = await _sessionService.GetSessionsByUserIdAsync(userId);
        return sessions.FirstOrDefault(s => IsActiveStatus(s.Status));
    }

    public IActionResult Index()
    {
        var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var suffix = string.IsNullOrEmpty(query) ? string.Empty : "&" + query.TrimStart('?');
        return Redirect($"/?focus=1{suffix}");
    }

    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartSessionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = await GetCurrentUserIdAsync();

        var activeSession = await GetActiveSessionDtoAsync(userId);
        if (activeSession is not null)
        {
            if (!request.CloseExisting)
            {
                return Conflict(new
                {
                    message = "Active session already exists.",
                    sessionId = activeSession.Id
                });
            }

            var now = DateTime.UtcNow;
            var actualDuration = now - activeSession.StartTime;
            if (actualDuration < TimeSpan.Zero) actualDuration = TimeSpan.Zero;

            await _sessionService.CompleteSessionAsync(new UpdateSessionDto
            {
                Id = activeSession.Id,
                Status = SessionStatus.Aborted.ToString(),
                EndTime = now,
                ActualDuration = actualDuration.ToString("c")
            });
        }

        var dto = new CreateSessionDto
        {
            UserId = userId,
            TaskId = request.TaskId,
            PlannedDuration = TimeSpan.FromMinutes(request.PlannedMinutes)
        };

        var sessionId = await _sessionService.StartSessionAsync(dto);
        return Ok(new { sessionId });
    }

    [HttpPost]
    public async Task<IActionResult> Complete([FromBody] UpdateSessionDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _sessionService.CompleteSessionAsync(dto);

        if (string.Equals(dto.Status, nameof(SessionStatus.Completed), StringComparison.OrdinalIgnoreCase))
        {
            var session = await _sessionService.GetSessionByIdAsync(dto.Id);
            if (session?.TaskId is int taskId)
            {
                var userId = await GetCurrentUserIdAsync();
                if (session.UserId == userId)
                {
                    await _taskService.DeleteTaskAsync(taskId);
                }
            }
        }

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Pause([FromBody] int sessionId)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _sessionService.PauseSessionAsync(sessionId);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Resume([FromBody] int sessionId)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _sessionService.ResumeSessionAsync(sessionId);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetUserSessions()
    {
        var userId = await GetCurrentUserIdAsync();
        var sessions = await _sessionService.GetSessionsByUserIdAsync(userId);
        return Json(sessions);
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveSession()
    {
        var userId = await GetCurrentUserIdAsync();
        var activeSession = await GetActiveSessionDtoAsync(userId);

        if (activeSession is null)
            return Json(new { hasActive = false });

        var plannedSeconds = (int)Math.Ceiling(activeSession.PlannedDuration.TotalSeconds);
        if (plannedSeconds < 1) plannedSeconds = 1;

        var elapsedSeconds = (int)Math.Floor((DateTime.UtcNow - activeSession.StartTime).TotalSeconds);
        var remainingSeconds = Math.Max(0, plannedSeconds - elapsedSeconds);

        return Json(new
        {
            hasActive = true,
            sessionId = activeSession.Id,
            taskId = activeSession.TaskId,
            status = activeSession.Status,
            plannedSeconds,
            remainingSeconds,
            label = activeSession.TaskTitle ?? string.Empty
        });
    }
}

public sealed class StartSessionRequest
{
    public int? TaskId { get; set; }
    public int PlannedMinutes { get; set; }
    public bool CloseExisting { get; set; }
}
using FocusSpace.Application.DTOs;
using FocusSpace.Application.Interfaces;
using FocusSpace.Application.Services;
using FocusSpace.Domain.Entities;
using FocusSpace.Domain.Enums;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace FocusSpace.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="SessionService"/>.
    /// Follows Arrange / Act / Assert pattern.
    /// </summary>
    public class SessionServiceTests
    {
        private static SessionService CreateService(Mock<ISessionRepository> repoMock) =>
            new(repoMock.Object);

        private static Session BuildSession(
            int id = 1,
            int userId = 10,
            int? taskId = 1,
            SessionStatus status = SessionStatus.Ongoing) =>
            new()
            {
                Id = id,
                UserId = userId,
                TaskId = taskId,
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = null,
                PlannedDuration = TimeSpan.FromMinutes(60),
                ActualDuration = null,
                Status = status,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

        // ?????????????????????????????????????????????????????????????
        // StartSessionAsync
        // ?????????????????????????????????????????????????????????????

        [Fact]
        public async Task StartSessionAsync_ValidDto_CreatesAndReturnId()
        {
            // Arrange
            var dto = new CreateSessionDto
            {
                UserId = 10,
                TaskId = 1,
                PlannedDuration = TimeSpan.FromMinutes(60)
            };

            int sessionId = 42;
            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.AddAsync(It.IsAny<Session>()))
                    .Callback<Session>(s => s.Id = sessionId)
                    .Returns(Task.CompletedTask);
            repoMock.Setup(r => r.SaveChangesAsync())
                    .Returns(Task.CompletedTask);

            var service = CreateService(repoMock);

            // Act
            var result = await service.StartSessionAsync(dto);

            // Assert
            Assert.Equal(sessionId, result);
            repoMock.Verify(r => r.AddAsync(It.IsAny<Session>()), Times.Once);
            repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task StartSessionAsync_SessionCreated_HasCorrectStatus()
        {
            // Arrange
            var dto = new CreateSessionDto
            {
                UserId = 5,
                TaskId = 2,
                PlannedDuration = TimeSpan.FromMinutes(30)
            };

            Session capturedSession = null!;
            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.AddAsync(It.IsAny<Session>()))
                    .Callback<Session>(s => capturedSession = s)
                    .Returns(Task.CompletedTask);
            repoMock.Setup(r => r.SaveChangesAsync())
                    .Returns(Task.CompletedTask);

            var service = CreateService(repoMock);

            // Act
            await service.StartSessionAsync(dto);

            // Assert
            Assert.NotNull(capturedSession);
            Assert.Equal(SessionStatus.Ongoing, capturedSession.Status);
            Assert.Equal(5, capturedSession.UserId);
            Assert.Equal(2, capturedSession.TaskId);
        }

        [Fact]
        public async Task StartSessionAsync_WithoutTask_TaskIdCanBeNull()
        {
            // Arrange
            var dto = new CreateSessionDto
            {
                UserId = 10,
                TaskId = null,
                PlannedDuration = TimeSpan.FromMinutes(45)
            };

            Session capturedSession = null!;
            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.AddAsync(It.IsAny<Session>()))
                    .Callback<Session>(s => capturedSession = s)
                    .Returns(Task.CompletedTask);
            repoMock.Setup(r => r.SaveChangesAsync())
                    .Returns(Task.CompletedTask);

            var service = CreateService(repoMock);

            // Act
            await service.StartSessionAsync(dto);

            // Assert
            Assert.Null(capturedSession.TaskId);
        }

        // ?????????????????????????????????????????????????????????????
        // CompleteSessionAsync
        // ?????????????????????????????????????????????????????????????

        [Fact]
        public async Task CompleteSessionAsync_ExistingSession_UpdatesStatus()
        {
            // Arrange
            var session = BuildSession(id: 5, status: SessionStatus.Ongoing);
            var dto = new UpdateSessionDto
            {
                Id = 5,
                Status = SessionStatus.Completed.ToString(),
                EndTime = DateTime.UtcNow,
                ActualDuration = "01:00:00"
            };

            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(session);
            repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = CreateService(repoMock);

            // Act
            await service.CompleteSessionAsync(dto);

            // Assert
            Assert.Equal(SessionStatus.Completed, session.Status);
            Assert.NotNull(session.EndTime);
            Assert.Equal(TimeSpan.FromHours(1), session.ActualDuration);
            repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CompleteSessionAsync_SessionNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dto = new UpdateSessionDto
            {
                Id = 999,
                Status = SessionStatus.Completed.ToString(),
                EndTime = DateTime.UtcNow
            };

            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

            var service = CreateService(repoMock);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CompleteSessionAsync(dto));
        }

        [Fact]
        public async Task CompleteSessionAsync_NullActualDuration_SetsToNull()
        {
            // Arrange
            var session = BuildSession(id: 6, status: SessionStatus.Ongoing);
            var dto = new UpdateSessionDto
            {
                Id = 6,
                Status = SessionStatus.Aborted.ToString(),
                EndTime = DateTime.UtcNow,
                ActualDuration = null
            };

            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(session);
            repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = CreateService(repoMock);

            // Act
            await service.CompleteSessionAsync(dto);

            // Assert
            Assert.Null(session.ActualDuration);
            Assert.Equal(SessionStatus.Aborted, session.Status);
        }

        // ?????????????????????????????????????????????????????????????
        // PauseSessionAsync
        // ?????????????????????????????????????????????????????????????

        [Fact]
        public async Task PauseSessionAsync_OngoingSession_ChangeStatusToPaused()
        {
            // Arrange
            var session = BuildSession(id: 7, status: SessionStatus.Ongoing);
            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(session);
            repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = CreateService(repoMock);

            // Act
            await service.PauseSessionAsync(7);

            // Assert
            Assert.Equal(SessionStatus.Paused, session.Status);
            repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task PauseSessionAsync_SessionNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

            var service = CreateService(repoMock);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.PauseSessionAsync(999));
        }

        [Theory]
        [InlineData(SessionStatus.Paused)]
        [InlineData(SessionStatus.Completed)]
        [InlineData(SessionStatus.Aborted)]
        public async Task PauseSessionAsync_NonOngoingSession_ThrowsInvalidOperationException(
            SessionStatus status)
        {
            // Arrange
            var session = BuildSession(id: 8, status: status);
            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(session);

            var service = CreateService(repoMock);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.PauseSessionAsync(8));
        }

        // ?????????????????????????????????????????????????????????????
        // ResumeSessionAsync
        // ?????????????????????????????????????????????????????????????

        [Fact]
        public async Task ResumeSessionAsync_PausedSession_ChangeStatusToOngoing()
        {
            // Arrange
            var session = BuildSession(id: 9, status: SessionStatus.Paused);
            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(session);
            repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = CreateService(repoMock);

            // Act
            await service.ResumeSessionAsync(9);

            // Assert
            Assert.Equal(SessionStatus.Ongoing, session.Status);
            repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ResumeSessionAsync_SessionNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

            var service = CreateService(repoMock);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ResumeSessionAsync(999));
        }

        [Theory]
        [InlineData(SessionStatus.Ongoing)]
        [InlineData(SessionStatus.Completed)]
        [InlineData(SessionStatus.Aborted)]
        public async Task ResumeSessionAsync_NonPausedSession_ThrowsInvalidOperationException(
            SessionStatus status)
        {
            // Arrange
            var session = BuildSession(id: 10, status: status);
            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(session);

            var service = CreateService(repoMock);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ResumeSessionAsync(10));
        }

        [Fact]
        public async Task GetFocusRecommendationAsync_NoCompletedSessions_ReturnsDefaultRecommendation()
        {
            // Arrange
            var sessions = new List<Session>
            {
                BuildSession(id: 11, status: SessionStatus.Ongoing),
                BuildSession(id: 12, status: SessionStatus.Paused)
            };

            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sessions);

            var service = CreateService(repoMock);

            // Act
            var result = await service.GetFocusRecommendationAsync(10);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(25), result.RecommendedDuration);
            Assert.Equal("Any time", result.BestFocusPeriod);
            Assert.Contains("Not enough completed sessions", result.Details);
        }

        [Fact]
        public async Task GetFocusRecommendationAsync_WithCompletedSessions_ReturnsComputedRecommendation()
        {
            // Arrange
            var sessions = new List<Session>
            {
                new Session
                {
                    Id = 13,
                    UserId = 10,
                    StartTime = new DateTime(2026, 5, 4, 9, 15, 0),
                    EndTime = new DateTime(2026, 5, 4, 10, 15, 0),
                    PlannedDuration = TimeSpan.FromMinutes(60),
                    ActualDuration = TimeSpan.FromMinutes(60),
                    Status = SessionStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                },
                new Session
                {
                    Id = 14,
                    UserId = 10,
                    StartTime = new DateTime(2026, 5, 5, 9, 45, 0),
                    EndTime = new DateTime(2026, 5, 5, 10, 30, 0),
                    PlannedDuration = TimeSpan.FromMinutes(45),
                    ActualDuration = TimeSpan.FromMinutes(45),
                    Status = SessionStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new Session
                {
                    Id = 15,
                    UserId = 10,
                    StartTime = new DateTime(2026, 5, 5, 14, 0, 0),
                    EndTime = new DateTime(2026, 5, 5, 15, 0, 0),
                    PlannedDuration = TimeSpan.FromMinutes(60),
                    ActualDuration = TimeSpan.FromMinutes(60),
                    Status = SessionStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
            };

            var repoMock = new Mock<ISessionRepository>();
            repoMock.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(sessions);

            var service = CreateService(repoMock);

            // Act
            var result = await service.GetFocusRecommendationAsync(10);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(55), result.RecommendedDuration);
            Assert.Equal("09:00 - 10:00", result.BestFocusPeriod);
            Assert.Contains("Based on 3 completed session(s)", result.Details);
        }
    }
}

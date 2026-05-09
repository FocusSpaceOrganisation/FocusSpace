using FocusSpace.Api.Controllers;
using FocusSpace.Application.DTOs;
using FocusSpace.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using User = FocusSpace.Domain.Entities.User;

namespace FocusSpace.Tests.Controllers
{
    /// <summary>
    /// Unit tests for <see cref="SessionController"/>.
    /// </summary>
    public class SessionControllerTests
    {
        private static UserManager<User> CreateUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                store.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object).Object;
        }

        private static SessionController CreateController(Mock<ISessionService>? serviceMock = null, UserManager<User>? userManager = null, Mock<ITaskService>? taskServiceMock = null)
        {
            serviceMock ??= new Mock<ISessionService>();
            userManager ??= CreateUserManager();
            taskServiceMock ??= new Mock<ITaskService>();
            return new SessionController(serviceMock.Object, userManager, taskServiceMock.Object);
        }

        // ═════════════════════════════════════════════════════════════
        // Index
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public void Index_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        // ═════════════════════════════════════════════════════════════
        // Start
        // ═════════════════════════════════════════════════════════════



        // ═════════════════════════════════════════════════════════════
        // Complete
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public async Task Complete_ValidDto_ReturnsOk()
        {
            // Arrange
            var serviceMock = new Mock<ISessionService>();
            var dto = new UpdateSessionDto { Id = 1, Status = "Completed", EndTime = DateTime.UtcNow };
            serviceMock.Setup(s => s.CompleteSessionAsync(It.IsAny<UpdateSessionDto>()))
                .Returns(Task.CompletedTask);

            var controller = CreateController(serviceMock);

            // Act
            var result = await controller.Complete(dto);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            serviceMock.Verify(s => s.CompleteSessionAsync(It.IsAny<UpdateSessionDto>()), Times.Once);
        }

        [Fact]
        public async Task Complete_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var serviceMock = new Mock<ISessionService>();
            var controller = CreateController(serviceMock);
            controller.ModelState.AddModelError("SessionId", "SessionId is required");
            var dto = new UpdateSessionDto();

            // Act
            var result = await controller.Complete(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        // ═════════════════════════════════════════════════════════════
        // Pause
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public async Task Pause_ValidSessionId_ReturnsOk()
        {
            // Arrange
            var serviceMock = new Mock<ISessionService>();
            int sessionId = 1;
            serviceMock.Setup(s => s.PauseSessionAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var controller = CreateController(serviceMock);

            // Act
            var result = await controller.Pause(sessionId);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            serviceMock.Verify(s => s.PauseSessionAsync(sessionId), Times.Once);
        }

        [Fact]
        public async Task Pause_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var serviceMock = new Mock<ISessionService>();
            var controller = CreateController(serviceMock);
            controller.ModelState.AddModelError("sessionId", "Invalid session ID");
            int sessionId = 0;

            // Act
            var result = await controller.Pause(sessionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        // ═════════════════════════════════════════════════════════════
        // Resume
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public async Task Resume_ValidSessionId_ReturnsOk()
        {
            // Arrange
            var serviceMock = new Mock<ISessionService>();
            int sessionId = 1;
            serviceMock.Setup(s => s.ResumeSessionAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var controller = CreateController(serviceMock);

            // Act
            var result = await controller.Resume(sessionId);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            serviceMock.Verify(s => s.ResumeSessionAsync(sessionId), Times.Once);
        }

        [Fact]
        public async Task Resume_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var serviceMock = new Mock<ISessionService>();
            var controller = CreateController(serviceMock);
            controller.ModelState.AddModelError("sessionId", "Invalid session ID");
            int sessionId = 0;

            // Act
            var result = await controller.Resume(sessionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }
    }
}

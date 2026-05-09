using FocusSpace.Api.Controllers;
using FocusSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FocusSpace.Tests.Controllers
{
    /// <summary>
    /// Unit tests for <see cref="HomeController"/>.
    /// </summary>
    public class HomeControllerTests
    {
        private static HomeController CreateController()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Planets.Add(new FocusSpace.Domain.Entities.Planet
            {
                Id = 3,
                Name = "Earth",
                OrderNumber = 3,
                Description = "Our home planet"
            });
            context.SaveChanges();

            return new HomeController(context);
        }

        [Fact]
        public async Task Index_ReturnsView_WithCorrectViewName()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Default view name is null (uses action name)
        }

        [Fact]
        public async Task Index_ReturnsViewResultType_IsIActionResult()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
        }

        [Fact]
        public void HomeController_CanBeInstantiated_Successfully()
        {
            // Act
            var controller = CreateController();

            // Assert
            Assert.NotNull(controller);
            Assert.IsAssignableFrom<Controller>(controller);
        }
    }
}

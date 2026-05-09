using FocusSpace.Api.Controllers;
using FocusSpace.Application.DTOs;
using FocusSpace.Application.Interfaces;
using FocusSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using User = FocusSpace.Domain.Entities.User;

namespace FocusSpace.Tests.Controllers
{
    /// <summary>
    /// Unit tests for <see cref="AccountController"/>.
    /// </summary>
    public class AccountControllerTests
    {
        // ── Fixture helpers ───────────────────────────────────────────

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

        private static SignInManager<User> CreateSignInManager(UserManager<User>? userManager = null)
        {
            userManager ??= CreateUserManager();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var schemes = new Mock<IAuthenticationSchemeProvider>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();

            return new Mock<SignInManager<User>>(
                userManager,
                contextAccessor.Object,
                claimsFactory.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<ILogger<SignInManager<User>>>().Object,
                schemes.Object,
                new Mock<IUserConfirmation<User>>().Object).Object;
        }

        private static AccountController CreateController(
            UserManager<User>? userManager = null,
            SignInManager<User>? signInManager = null,
            IEmailService? emailService = null,
            AppDbContext? context = null,
            IWebHostEnvironment? webHostEnvironment = null,
            ILogger<AccountController>? logger = null)
        {
            userManager ??= CreateUserManager();
            signInManager ??= CreateSignInManager(userManager);
            emailService ??= new Mock<IEmailService>().Object;

            if (context == null)
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

                context = new AppDbContext(options);
                context.Planets.Add(new FocusSpace.Domain.Entities.Planet
                {
                    Id = 3,
                    Name = "Earth",
                    OrderNumber = 3,
                    Description = "Our home planet"
                });
                context.SaveChanges();
            }
            
            // Set up IWebHostEnvironment properly
            if (webHostEnvironment == null)
            {
                var envMock = new Mock<IWebHostEnvironment>();
                envMock.Setup(w => w.EnvironmentName).Returns("Production");
                webHostEnvironment = envMock.Object;
            }
            
            logger ??= new Mock<ILogger<AccountController>>().Object;

            var controller = new AccountController(
                userManager,
                signInManager,
                emailService,
                context,
                webHostEnvironment,
                logger);

            // Set up HTTP context
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        private static User BuildUser(int id = 1, string email = "test@example.com", string username = "testuser")
        {
            return new User
            {
                Id = id,
                UserName = username,
                Email = email,
                EmailConfirmed = false,
                IsApproved = false,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };
        }

        // ═════════════════════════════════════════════════════════════
        // Register GET
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public void Register_Get_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.Register();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        // ═════════════════════════════════════════════════════════════
        // Register POST
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public async Task Register_Post_InvalidModel_ReturnsViewWithDto()
        {
            // Arrange
            var controller = CreateController();
            controller.ModelState.AddModelError("Username", "Username is required");

            var dto = new RegisterDto();

            // Act
            var result = await controller.Register(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
        }

        [Fact]
        public async Task Register_Post_DuplicateUsername_ReturnsViewWithError()
        {
            // Arrange
            var existingUser = BuildUser(username: "testuser");
            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock
                .Setup(m => m.FindByNameAsync("testuser"))
                .ReturnsAsync(existingUser);

            var controller = CreateController(userManagerMock.Object);
            var dto = new RegisterDto
            {
                Username = "testuser",
                Email = "new@example.com",
                Password = "SecurePass123!",
                ConfirmPassword = "SecurePass123!"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
            Assert.True(controller.ModelState.ContainsKey("Username"));
        }

        [Fact]
        public async Task Register_Post_CreateUserFails_ReturnsViewWithErrors()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock
                .Setup(m => m.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var errorResult = IdentityResult.Failed(new IdentityError { Description = "Password too weak" });
            userManagerMock
                .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(errorResult);

            var controller = CreateController(userManagerMock.Object);
            var dto = new RegisterDto
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "weak",
                ConfirmPassword = "weak"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
        }

        // ═════════════════════════════════════════════════════════════
        // RegisterConfirmation
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public void RegisterConfirmation_Get_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.RegisterConfirmation();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        // ═════════════════════════════════════════════════════════════
        // ConfirmEmail
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public async Task ConfirmEmail_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock
                .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var controller = CreateController(userManagerMock.Object);

            // Act
            var result = await controller.ConfirmEmail(999, "token");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // ═════════════════════════════════════════════════════════════
        // Login GET
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public void Login_Get_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.Login();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        // ═════════════════════════════════════════════════════════════
        // Login POST
        // ═════════════════════════════════════════════════════════════

        [Fact]
        public async Task Login_Post_InvalidModel_ReturnsViewWithDto()
        {
            // Arrange
            var controller = CreateController();
            controller.ModelState.AddModelError("Email", "Email is required");

            var dto = new LoginDto();

            // Act
            var result = await controller.Login(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_UserNotFound_ReturnsViewWithError()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock
                .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var controller = CreateController(userManagerMock.Object);
            var dto = new LoginDto { Email = "notfound@example.com", Password = "pass" };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
            Assert.True(controller.ModelState.ContainsKey(""));
        }

        [Fact]
        public async Task Login_Post_UserNotApproved_ReturnsViewWithError()
        {
            // Arrange
            var user = BuildUser(1, "test@example.com");
            user.IsApproved = false;
            user.EmailConfirmed = true;

            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock
                .Setup(m => m.FindByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            var controller = CreateController(userManagerMock.Object);
            var dto = new LoginDto { Email = "test@example.com", Password = "password" };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_EmailNotConfirmed_ReturnsViewWithError()
        {
            // Arrange
            var user = BuildUser(1, "test@example.com");
            user.IsApproved = true;
            user.EmailConfirmed = false;

            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock
                .Setup(m => m.FindByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            var controller = CreateController(userManagerMock.Object);
            var dto = new LoginDto { Email = "test@example.com", Password = "password" };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_InvalidPassword_ReturnsViewWithError()
        {
            // Arrange
            var user = BuildUser(1, "test@example.com");
            user.IsApproved = true;
            user.EmailConfirmed = true;

            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock
                .Setup(m => m.FindByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            var signInManagerMock = new Mock<SignInManager<User>>(
                userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<ILogger<SignInManager<User>>>().Object,
                new Mock<IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<User>>().Object);

            signInManagerMock
                .Setup(m => m.PasswordSignInAsync(user.UserName!, "wrongpass", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var controller = CreateController(
                userManagerMock.Object,
                signInManagerMock.Object);

            var dto = new LoginDto { Email = "test@example.com", Password = "wrongpass" };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_BlockedAccount_ReturnsViewWithError()
        {
            // Arrange
            var user = BuildUser(1, "test@example.com");
            user.IsApproved = true;
            user.EmailConfirmed = true;
            user.IsBlocked = true;

            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock
                .Setup(m => m.FindByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            var controller = CreateController(userManagerMock.Object);
            var dto = new LoginDto { Email = "test@example.com", Password = "password" };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_LockedAccount_ReturnsViewWithError()
        {
            // Arrange
            var user = BuildUser(1, "test@example.com");
            user.IsApproved = true;
            user.EmailConfirmed = true;

            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock
                .Setup(m => m.FindByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            var signInManagerMock = new Mock<SignInManager<User>>(
                userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<ILogger<SignInManager<User>>>().Object,
                new Mock<IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<User>>().Object);

            signInManagerMock
                .Setup(m => m.PasswordSignInAsync(user.UserName!, "password", false, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            var controller = CreateController(userManagerMock.Object, signInManagerMock.Object);
            var dto = new LoginDto { Email = "test@example.com", Password = "password" };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
        }

        [Fact]
        public async Task Logout_SignsOutAndRedirects()
        {
            // Arrange
            var signInManagerMock = new Mock<SignInManager<User>>(
                CreateUserManager(),
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<ILogger<SignInManager<User>>>().Object,
                new Mock<IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<User>>().Object);

            signInManagerMock.Setup(m => m.SignOutAsync()).Returns(Task.CompletedTask);

            var controller = CreateController(signInManager: signInManagerMock.Object);

            // Act
            var result = await controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Tasks", redirectResult.ControllerName);
            signInManagerMock.Verify(m => m.SignOutAsync(), Times.Once);
        }

        [Fact]
        public void ForgotPassword_Get_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.ForgotPassword();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task ForgotPassword_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var controller = CreateController();
            controller.ModelState.AddModelError("Email", "Email is required");

            var dto = new ForgotPasswordDto();

            // Act
            var result = await controller.ForgotPassword(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
        }

        [Fact]
        public void ForgotPasswordConfirmation_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.ForgotPasswordConfirmation();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public void ResetPassword_Get_ReturnsViewWithDto()
        {
            // Arrange
            var controller = CreateController();
            const int userId = 123;
            const string token = "test-token";

            // Act
            var result = controller.ResetPassword(userId, token);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
            var dto = Assert.IsType<ResetPasswordDto>(viewResult.Model);
            Assert.Equal(userId.ToString(), dto.UserId);
            Assert.Equal(token, dto.Token);
        }

        [Fact]
        public async Task ResetPassword_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var controller = CreateController();
            controller.ModelState.AddModelError("NewPassword", "Password is required");

            var dto = new ResetPasswordDto { UserId = "1", Token = "token" };

            // Act
            var result = await controller.ResetPassword(dto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(dto, viewResult.Model);
        }

        [Fact]
        public async Task ResetPassword_Post_UserNotFound_RedirectsWithoutError()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);

            userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            var controller = CreateController(userManager: userManagerMock.Object);
            var dto = new ResetPasswordDto { UserId = "999", Token = "token", NewPassword = "NewPass123!" };

            // Act
            var result = await controller.ResetPassword(dto);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ResetPasswordConfirmation", redirectResult.ActionName);
        }

        [Fact]
        public void ResetPasswordConfirmation_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.ResetPasswordConfirmation();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public void AccessDenied_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.AccessDenied();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }
    }
}

using FocusSpace.Application.DTOs;
using FocusSpace.Application.Interfaces;
using FocusSpace.Domain.Entities;
using FocusSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusSpace.Api.Controllers
{
    /// <summary>
    /// Handles registration, login, logout, and password management.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;

        private readonly IWebHostEnvironment _env;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailService emailService,
            AppDbContext context,
            IWebHostEnvironment env,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _context = context;
            _env = env;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════
        // REGISTER
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var defaultPlanet = await _context.Planets
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower() == "earth");

            if (defaultPlanet is null)
            {
                ModelState.AddModelError(string.Empty, "Default planet Earth was not found. Contact administrator.");
                return View(dto);
            }

            // Check for duplicate username
            if (await _userManager.FindByNameAsync(dto.Username) is not null)
            {
                ModelState.AddModelError("Username", "This username is already taken.");
                return View(dto);
            }

            var user = new User
            {
                UserName = dto.Username,
                Email = dto.Email,
                IsApproved = _env.IsDevelopment(), // у dev одразу approved
                EmailConfirmed = _env.IsDevelopment(), // у dev одразу confirmed
                CreatedAt = DateTime.UtcNow,
                CurrentPlanetId = defaultPlanet.Id
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(dto);
            }

            await _userManager.AddToRoleAsync(user, "User");

            // Відправляємо email тільки якщо не dev
            if (!_env.IsDevelopment())
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(
                    nameof(ConfirmEmail), "Account",
                    new { userId = user.Id, token },
                    Request.Scheme)!;

                try
                {
                    await _emailService.SendConfirmationEmailAsync(user.Email!, user.UserName!, confirmationLink);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                }

                TempData["Info"] = "Registration successful! Please confirm your email and wait for admin approval.";
                return RedirectToAction(nameof(RegisterConfirmation));
            }

            // У dev одразу редиректимо на логін
            TempData["Success"] = "Dev mode: account created and auto-approved. You can log in now.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterConfirmation() => View();

        // ══════════════════════════════════════════════════════════════
        // EMAIL CONFIRMATION
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed for user {UserId}", userId);
                TempData["Success"] = "Your email has been confirmed! An admin will review and approve your account shortly.";
                return RedirectToAction(nameof(Login));
            }

            TempData["Error"] = "Email confirmation failed. The link may have expired.";
            return RedirectToAction(nameof(Login));
        }

        // ══════════════════════════════════════════════════════════════
        // LOGIN
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
            public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(dto);

            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(dto);
            }

            // Email must be confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Please confirm your email before logging in.");
                return View(dto);
            }

            // Account must be approved by admin
            if (!user.IsApproved)
            {
                ModelState.AddModelError(string.Empty, "Your account is awaiting admin approval.");
                return View(dto);
            }

            // Account must not be blocked
            if (user.IsBlocked)
            {
                ModelState.AddModelError(string.Empty, "Your account has been blocked. Contact support for assistance.");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, dto.Password,
                isPersistent: dto.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} logged in", user.Id);
                return LocalRedirect(returnUrl ?? Url.Action("Index", "Tasks")!);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {UserId} account is locked out", user.Id);
                ModelState.AddModelError(string.Empty, "Account is temporarily locked due to too many failed attempts. Try again later.");
                return View(dto);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(dto);
        }

        // ══════════════════════════════════════════════════════════════
        // LOGOUT
        // ══════════════════════════════════════════════════════════════

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User signed out");
            return RedirectToAction("Index", "Tasks");
        }

        // ══════════════════════════════════════════════════════════════
        // FORGOT PASSWORD
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = await _userManager.FindByEmailAsync(dto.Email);

            // Always show success — never reveal whether email exists (security)
            if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Info"] = "If that email is registered and confirmed, you will receive a reset link shortly.";
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action(
                nameof(ResetPassword), "Account",
                new { userId = user.Id, token },
                Request.Scheme)!;

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email!, user.UserName!, resetLink);
                _logger.LogInformation("Password reset email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }

            TempData["Info"] = "If that email is registered and confirmed, you will receive a reset link shortly.";
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        // ══════════════════════════════════════════════════════════════
        // RESET PASSWORD
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(int userId, string token)
        {
            var dto = new ResetPasswordDto
            {
                UserId = userId.ToString(),
                Token = token
            };
            return View(dto);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user is null)
            {
                // Do not reveal user existence
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(dto);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();

        // ══════════════════════════════════════════════════════════════
        // ACCESS DENIED
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
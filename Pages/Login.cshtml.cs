using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthDbContext _context;
        private readonly EmailService _emailService; // Inject your EmailService
        private readonly IConfiguration _configuration;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AuthDbContext context, EmailService emailService
            , IConfiguration configuration
            )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _emailService = emailService; // Initialize your EmailService here
            _configuration = configuration;
        }

        [BindProperty]
        public LoginVM LModel { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync(string RecaptchaToken)
        {
            // 1. Verify reCaptcha
            var isHuman = await VerifyRecaptcha(RecaptchaToken);
            if (!isHuman)
            {
                ModelState.AddModelError("", "Bot activity detected. Please try again.");
                return Page();
            }
            if (!ModelState.IsValid) return Page();
            LModel.Email = LModel.Email?.Trim().ToLower();  

            var result = await _signInManager.PasswordSignInAsync(
                LModel.Email,
                LModel.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(LModel.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Login succeeded but user record not found.");
                    return Page();
                }

                // --- NEW: 2FA OTP GENERATION ---
                // Generate a random 6-digit code
                var otp = new Random().Next(100000, 999999).ToString();
                user.TwoFactorCode = otp; // Ensure this property exists in your ApplicationUser model
                await _userManager.UpdateAsync(user);

                // Send the OTP via your MailKit EmailService
                // Make sure you have private readonly EmailService _emailService; injected in your constructor
                await _emailService.SendEmailAsync(user.Email, "Your 2FA Security Code",
                    $"<h3>Security Verification</h3><p>Your 6-digit code is: <b>{otp}</b></p>");
                // -------------------------------

                var sessionId = Guid.NewGuid().ToString();
                user.SessionId = sessionId;
                await _userManager.UpdateAsync(user);

                await _signInManager.SignOutAsync();

                var claims = new List<Claim>
    {
        new Claim("SessionId", sessionId)
    };

                await _signInManager.SignInWithClaimsAsync(
                    user,
                    isPersistent: false,
                    claims);

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user?.Id ?? "UNKNOWN",
                    Action = "LOGIN_SUCCESS",
                    IpAddress = ip
                });

                await _context.SaveChangesAsync();

                // Check password expiration
                if (user.LastPasswordChanged.AddMinutes(2) < DateTime.UtcNow)
                {
                    TempData["FlashMessage"] = "Your password has expired. Please update it.";
                    // We still go to 2FA first to secure the session
                }

                // REDIRECT TO 2FA VERIFICATION PAGE INSTEAD OF INDEX
                return RedirectToPage("/Verify2FA");
            }

            if (result.IsLockedOut)
            {
                // Try to log even if locked (user might still exist)
                var user = await _userManager.FindByEmailAsync(LModel.Email);

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user?.Id ?? "UNKNOWN",
                    Action = "LOGIN_LOCKED",
                    IpAddress = ip
                });
                await _context.SaveChangesAsync();

                ModelState.AddModelError("", "Account locked due to multiple failed login attempts. Try again later.");
                return Page();
            }

            // Invalid credentials
            {
                var user = await _userManager.FindByEmailAsync(LModel.Email);

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user?.Id ?? "UNKNOWN",
                    Action = "LOGIN_FAIL",
                    IpAddress = ip
                });
                await _context.SaveChangesAsync();
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return Page();
        }

        private async Task<bool> VerifyRecaptcha(string token)
        {
            string secret = _configuration["Recaptcha:SecretKey"];
            using var client = new HttpClient();
            var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={token}", null);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"RECAPTCHA RESPONSE: {jsonResponse}");

            // In a real app, use a JSON parser. For a quick demo, this works:
            return jsonResponse.Contains("\"success\": true");
        }
    }

    public class LoginVM
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
    }
}

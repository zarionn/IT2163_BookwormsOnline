using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthDbContext _context;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, AuthDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public ResetInput Input { get; set; }

        public class ResetInput
        {
            public string Email { get; set; }
            public string Token { get; set; }

            [Required, MinLength(12)]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$@$!%*?&])[A-Za-z\d$@$!%*?&]{12,}$")]
            public string NewPassword { get; set; }
        }

        public void OnGet(string token, string email)
        {
            Input = new ResetInput { Token = token, Email = email };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null) return RedirectToPage("/Login");

            // 1. CHECK PASSWORD HISTORY (Requirement: Avoid reuse of last 2)
            var history = await _context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.ChangedDate)
                .Take(2).ToListAsync();

            foreach (var oldPass in history)
            {
                var verify = _userManager.PasswordHasher.VerifyHashedPassword(user, oldPass.PasswordHash, Input.NewPassword);
                if (verify != PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "You cannot reuse your last 2 passwords.");
                    return Page();
                }
            }

            // 2. ACTUALLY RESET
            var result = await _userManager.ResetPasswordAsync(user, Input.Token, Input.NewPassword);
            if (result.Succeeded)
            {
                // Save current hash to history before it's gone
                _context.PasswordHistories.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = user.PasswordHash,
                    ChangedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                return RedirectToPage("/Login");
            }

            return Page();
        }
    }
}

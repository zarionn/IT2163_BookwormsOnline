using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        // You MUST add this context to access the custom PasswordHistories table
        private readonly AuthDbContext _context;

        // Inject both UserManager and AuthDbContext
        public ChangePasswordModel(UserManager<ApplicationUser> userManager, AuthDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public ChangePasswordInput Input { get; set; }

        public class ChangePasswordInput
        {
            [Required, DataType(DataType.Password)]
            public string OldPassword { get; set; }

            [Required, MinLength(12)] // Securing credential task: Min 12 chars 
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$@$!%*?&])[A-Za-z\d$@$!%*?&]{12,}$",
             ErrorMessage = "Password must be at least 12 chars with uppercase, lowercase, number and special character.")]
            public string NewPassword { get; set; }

            [Required, Compare("NewPassword"), DataType(DataType.Password)]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: OnPostAsync started.");

            // 1️⃣ Check if model is valid
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ModelState is INVALID.");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"DEBUG: ModelState Error - {key}: {error.ErrorMessage}");
                    }
                }
                return Page();
            }
            System.Diagnostics.Debug.WriteLine("DEBUG: ModelState is valid.");

            // 2️⃣ Get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: User is null. Redirecting to login.");
                return RedirectToPage("/Login");
            }
            System.Diagnostics.Debug.WriteLine($"DEBUG: User found: {user.UserName}, LastPasswordChanged: {user.LastPasswordChanged}");

            // 3️⃣ Min Age check
            if (user.LastPasswordChanged != default(DateTime) && user.LastPasswordChanged.AddMinutes(1) > DateTime.UtcNow)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: Failed Min Age Check. Last changed: {user.LastPasswordChanged}");
                ModelState.AddModelError("", "You changed your password too recently. Please wait 1 minute.");
                return Page();
            }
            System.Diagnostics.Debug.WriteLine("DEBUG: Min Age check passed.");

            // 4️⃣ Password History check
            var lastTwoPasswords = await _context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.ChangedDate)
                .Take(2)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"DEBUG: Found {lastTwoPasswords.Count} previous passwords in history.");
            foreach (var oldPass in lastTwoPasswords)
            {
                var result = _userManager.PasswordHasher.VerifyHashedPassword(user, oldPass.PasswordHash, Input.NewPassword);
                System.Diagnostics.Debug.WriteLine($"DEBUG: Checking against old password hash: {oldPass.PasswordHash} => Result: {result}");
                if (result != PasswordVerificationResult.Failed)
                {
                    System.Diagnostics.Debug.WriteLine("DEBUG: Failed Password Reuse Check.");
                    ModelState.AddModelError("", "You cannot reuse your last 2 passwords.");
                    return Page();
                }
            }
            System.Diagnostics.Debug.WriteLine("DEBUG: Password history check passed.");

            // 5️⃣ Attempt ChangePasswordAsync
            var oldHash = user.PasswordHash;
            System.Diagnostics.Debug.WriteLine("DEBUG: Attempting ChangePasswordAsync...");
            var changeResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);

            if (changeResult.Succeeded)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ChangePasswordAsync SUCCEEDED.");

                // Update last password changed
                var updatedUser = await _userManager.FindByIdAsync(user.Id);
                updatedUser.LastPasswordChanged = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(updatedUser);
                System.Diagnostics.Debug.WriteLine($"DEBUG: UpdateAsync Succeeded: {updateResult.Succeeded}");

                // Save old password to history
                _context.PasswordHistories.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = oldHash,
                    ChangedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("DEBUG: PasswordHistories saved to DB.");

                TempData["FlashMessage"] = "Password updated successfully!";
                return RedirectToPage("/Index");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ChangePasswordAsync FAILED.");
                foreach (var error in changeResult.Errors)
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: Identity Error: {error.Description}");
                }
            }

            System.Diagnostics.Debug.WriteLine("DEBUG: OnPostAsync ending, returning Page()");
            return Page();
        }

    }
}
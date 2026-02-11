using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    [Authorize] // Important: The user is technically "signed in" with claims, but stuck on this page
    public class Verify2FAModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public Verify2FAModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public string InputCode { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(InputCode))
            {
                ModelState.AddModelError("", "Please enter the code.");
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Login");

            // Check if the input matches the saved OTP
            if (user.TwoFactorCode == InputCode)
            {
                // Correct code! Clear it so it can't be reused
                user.TwoFactorCode = null;
                await _userManager.UpdateAsync(user);

                // Now decide where to send them
                if (TempData["FlashMessage"] != null)
                {
                    // If they had an expired password, send them to Change Password
                    return RedirectToPage("/ChangePassword", new { expired = true });
                }

                return RedirectToPage("/Index");
            }

            // Wrong code
            ModelState.AddModelError("", "Invalid code. Please check your email.");
            return Page();
        }
    }
}
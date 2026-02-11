using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthDbContext _context;

        public LogoutModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AuthDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (user != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "LOGOUT",
                    IpAddress = ip
                });

                await _context.SaveChangesAsync();
            }

            await _signInManager.SignOutAsync();
            return RedirectToPage("/Login");
        }
    }
}

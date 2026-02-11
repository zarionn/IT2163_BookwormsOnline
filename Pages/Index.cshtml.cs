using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataProtector _protector;
        private readonly AuthDbContext _context;

        public ApplicationUser CurrentUser { get; set; }
        public string DecryptedCreditCard { get; set; }

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            IDataProtectionProvider dataProtectionProvider,
            AuthDbContext context)
        {
            _userManager = userManager;
            _protector = dataProtectionProvider.CreateProtector("MySecretKey");
            _context = context;
        }

        public async Task OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser != null)
            {
                var claimSessionId = User.FindFirst("SessionId")?.Value;

                if (string.IsNullOrEmpty(claimSessionId) || CurrentUser.SessionId != claimSessionId)
                {
                    await HttpContext.SignOutAsync();
                    Response.Redirect("/Login");
                    return;
                }

                // Audit: user viewed homepage
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = CurrentUser.Id,
                    Action = "VIEW_HOME",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();

                // Decrypt credit card for display
                if (!string.IsNullOrEmpty(CurrentUser.CreditCard))
                {
                    DecryptedCreditCard = _protector.Unprotect(CurrentUser.CreditCard);
                }
            }
        }
    }
}

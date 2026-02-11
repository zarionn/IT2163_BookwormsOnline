using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.ViewModels;
using System.Text.Encodings.Web;

namespace WebApplication1.Pages
{
    public class RegisterModel : PageModel
    {
        private UserManager<ApplicationUser> userManager { get; }
        private SignInManager<ApplicationUser> signInManager { get; }

        [BindProperty]
        public Register RModel { get; set; }

        private readonly IDataProtector _protector;

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDataProtectionProvider dataProtectionProvider)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            _protector = dataProtectionProvider.CreateProtector("MySecretKey");
        }

        public void OnGet()
        {
        }

        

        //Save data into the database
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                RModel.FirstName = RModel.FirstName?.Trim();
                RModel.LastName = RModel.LastName?.Trim();
                RModel.Email = RModel.Email?.Trim().ToLower();
                RModel.MobileNo = RModel.MobileNo?.Trim();
                RModel.BillingAddress = RModel.BillingAddress?.Trim();
                RModel.ShippingAddress = RModel.ShippingAddress?.Trim();
                RModel.CreditCard = RModel.CreditCard?.Trim();

                var encoder = HtmlEncoder.Default;
                RModel.BillingAddress = encoder.Encode(RModel.BillingAddress);
                RModel.ShippingAddress = encoder.Encode(RModel.ShippingAddress);

                if (RModel.Photo == null || RModel.Photo.Length == 0)
                {
                    ModelState.AddModelError("RModel.Photo", "Photo is required.");
                    return Page();
                }

                var ext = Path.GetExtension(RModel.Photo.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg")
                {
                    ModelState.AddModelError("RModel.Photo", "Only .JPG files are allowed.");
                    return Page();
                }

                if (RModel.Photo.Length > 2 * 1024 * 1024) // 2MB limit
                {
                    ModelState.AddModelError("RModel.Photo", "File size must not exceed 2MB.");
                    return Page();
                }

                if (!RModel.Photo.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError("RModel.Photo", "Invalid file type.");
                    return Page();
                }

                // Save photo into wwwroot/uploads
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await RModel.Photo.CopyToAsync(stream);
                }

                var photoPath = "/uploads/" + fileName;
                
                var user = new ApplicationUser()
                {
                    UserName = RModel.Email,
                    Email = RModel.Email,

                    FirstName = RModel.FirstName,
                    LastName = RModel.LastName,
                    MobileNo = RModel.MobileNo,
                    BillingAddress = RModel.BillingAddress,
                    ShippingAddress = RModel.ShippingAddress,
                    PhotoPath = photoPath,

                    CreditCard = _protector.Protect(RModel.CreditCard),
                    LastPasswordChanged = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(user, RModel.Password);
                if (result.Succeeded)
                {
                    await signInManager.SignInAsync(user, false); return RedirectToPage("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

            }
            return Page();
        }

    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.ViewModels
{
    public class Register
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First name can only contain letters.")]
        public string FirstName { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last name can only contain letters.")]
        public string LastName { get; set; }

        [Required]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Mobile number must be exactly 8 digits.")]
        public string MobileNo { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
    ErrorMessage = "Invalid email characters detected.")]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^\d{12,19}$", ErrorMessage = "Credit card must be numeric.")]
        public string CreditCard { get; set; }

        // ✅ Allow address characters, but block HTML tags by disallowing < and >
        [Required]
        [StringLength(200, ErrorMessage = "Billing address must be 200 characters or less.")]
        [RegularExpression(@"^[^<>\\]*$", ErrorMessage = "Billing address cannot contain < or > and backslash characters.")]
        public string BillingAddress { get; set; }

        // Shipping address requirement says allow all special chars,
        // but you can still block < and > to prevent HTML/script tags.
        [Required]
        [StringLength(200, ErrorMessage = "Shipping address must be 200 characters or less.")]
        [RegularExpression(@"^[^<>\\]*$", ErrorMessage = "Shipping address cannot contain < or > characters.")]
        public string ShippingAddress { get; set; }

        [Required]
        public IFormFile Photo { get; set; }

        [Required, DataType(DataType.Password)]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$@$!%*?&])[A-Za-z\d$@$!%*?&]{12,}$",
    ErrorMessage = "Password must be at least 12 characters long and include a combination of upper-case, lower-case, numbers, and special characters.")]
        public string Password { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}

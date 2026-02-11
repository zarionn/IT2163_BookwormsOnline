using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string? SessionId { get; set; }


        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string CreditCard { get; set; }

        public string MobileNo { get; set; }

        public string BillingAddress { get; set; }

        public string ShippingAddress { get; set; }

        public string PhotoPath{ get; set; }

        public DateTime LastPasswordChanged { get; set; } = DateTime.UtcNow;

        public string? TwoFactorCode { get; set; }
    }
}

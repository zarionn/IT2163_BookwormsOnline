using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model
{
    public class PasswordHistory
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string PasswordHash { get; set; }
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}
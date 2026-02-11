using System.ComponentModel.DataAnnotations;
namespace WebApplication1.Model
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Action { get; set; }  // e.g. "LOGIN_SUCCESS", "LOGIN_FAIL", "LOGOUT", "VIEW_HOME"

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? IpAddress { get; set; }
    }
}

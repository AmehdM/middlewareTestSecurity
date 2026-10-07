namespace MiddlewareDemo.Core.Models;

public sealed class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public bool IsAdmin { get; set; }
    public string NationalId { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string TenantId { get; set; } = "default";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

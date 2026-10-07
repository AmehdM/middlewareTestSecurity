namespace MiddlewareDemo.Core.Models;

public sealed class Customer
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string TenantId { get; set; } = "default";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace MiddlewareDemo.Api.Models;

public sealed class User
{
    public int Id { get; init; }
    public string Email { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public string Role { get; init; } = "user";
    public string NationalId { get; init; } = "";
    public decimal Salary { get; init; }
}

public sealed record UserProfile(int Id, string Email, string NationalId, decimal Salary);

public sealed record AdminUserView(int Id, string Email, string Role);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record ResetRequest(string? Email);

public sealed record Item(int Id, string Name, double Price);

public sealed record CreateItemRequest(string? Name, double Price);

public sealed record Order(int Id, string Customer, int ItemId, int Quantity);

public sealed record CreateOrderRequest(string? Customer, int ItemId, int Quantity);

public sealed record ImportRequest(string? Name, string? Description);

public sealed record MedicalClaim(string ClaimId, string Patient, string Diagnosis, double Amount);

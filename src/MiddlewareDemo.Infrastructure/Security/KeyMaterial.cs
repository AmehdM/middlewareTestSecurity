namespace MiddlewareDemo.Infrastructure.Security;

public static class KeyMaterial
{
    public const string TokenSigningKey = "lab-infra-signing-key-4d8e1f7a03bc92e6-demo";
    public const string DataKey = "lab-data-key-16b";
    public const string DataIv = "lab-iv-0123456789";
    public const string DefaultConnection = "Server=db-lab.internal;Database=MiddlewareDemo;User Id=svc_app;Password=Svc#Lab2024!;";
    public const string AdminPassword = "A12";
}

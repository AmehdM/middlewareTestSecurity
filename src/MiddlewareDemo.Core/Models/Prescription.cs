namespace MiddlewareDemo.Core.Models;

public sealed class Prescription
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientNationalId { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}

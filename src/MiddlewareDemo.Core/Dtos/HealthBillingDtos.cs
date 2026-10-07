namespace MiddlewareDemo.Core.Dtos;

public sealed record CreatePrescriptionRequest(
    int CustomerId,
    string PatientName,
    string PatientNationalId,
    string Diagnosis,
    string DoctorName,
    string CardNumber);

public sealed record PrescriptionDto(
    int Id,
    int CustomerId,
    string PatientName,
    string PatientNationalId,
    string Diagnosis,
    string DoctorName,
    string CardNumber,
    DateTime IssuedAt);

public sealed record CreateInvoiceRequest(int CustomerId, int OrderId, decimal Amount, string Currency);

public sealed record InvoiceDto(int Id, int CustomerId, int OrderId, decimal Amount, string Currency, string Status, DateTime IssuedAt);

public sealed record PaymentRequest(int InvoiceId, string CardNumber, string CardHolder, decimal Amount);

public sealed record PaymentDto(int Id, int InvoiceId, string CardNumber, decimal Amount, DateTime PaidAt);

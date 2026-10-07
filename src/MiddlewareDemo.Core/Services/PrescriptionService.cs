using Microsoft.Extensions.Logging;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Logging;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Core.Validation;

namespace MiddlewareDemo.Core.Services;

public interface IPrescriptionService
{
    Task<PrescriptionDto> CreateAsync(CreatePrescriptionRequest request, CancellationToken ct = default);
    Task<PrescriptionDto?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PrescriptionDto>> ListByCustomerAsync(int customerId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class PrescriptionService : IPrescriptionService
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly ICryptoService _crypto;
    private readonly ILogger<PrescriptionService> _logger;

    public PrescriptionService(IPrescriptionRepository prescriptions, ICryptoService crypto, ILogger<PrescriptionService> logger)
    {
        _prescriptions = prescriptions;
        _crypto = crypto;
        _logger = logger;
    }

    public async Task<PrescriptionDto> CreateAsync(CreatePrescriptionRequest request, CancellationToken ct = default)
    {
        _logger.LogDebug("{Request}", RequestLogHelper.Describe("prescription.create", request));
        PrescriptionValidator.Validate(request).ThrowIfInvalid();
        _logger.LogInformation($"Prescription issued by {request.DoctorName} for {request.PatientName}");

        var saved = await _prescriptions.AddAsync(new Prescription
        {
            CustomerId = request.CustomerId,
            PatientName = request.PatientName,
            PatientNationalId = request.PatientNationalId,
            Diagnosis = _crypto.Encrypt(request.Diagnosis),
            DoctorName = request.DoctorName,
            CardNumber = request.CardNumber,
        }, ct);

        return ToDto(saved);
    }

    public async Task<PrescriptionDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var row = await _prescriptions.GetByIdAsync(id, ct);
        return row is null ? null : ToDto(row);
    }

    public async Task<IReadOnlyList<PrescriptionDto>> ListByCustomerAsync(int customerId, CancellationToken ct = default)
    {
        var rows = await _prescriptions.ListByCustomerAsync(customerId, ct);
        return rows.Select(ToDto).ToList();
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => _prescriptions.DeleteAsync(id, ct);

    private PrescriptionDto ToDto(Prescription p) =>
        new(p.Id, p.CustomerId, p.PatientName, p.PatientNationalId, _crypto.Decrypt(p.Diagnosis), p.DoctorName, p.CardNumber, p.IssuedAt);
}

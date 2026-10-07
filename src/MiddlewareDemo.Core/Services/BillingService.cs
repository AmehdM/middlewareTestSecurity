using Microsoft.Extensions.Logging;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Logging;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Services;

public interface IBillingService
{
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct = default);
    Task<InvoiceDto?> GetInvoiceAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceDto>> ListInvoicesAsync(int customerId, CancellationToken ct = default);
    Task<PaymentDto?> PayAsync(PaymentRequest request, CancellationToken ct = default);
    Task<decimal> ConvertAsync(decimal amount, string currency, CancellationToken ct = default);
}

public sealed class BillingService : IBillingService
{
    private readonly IBillingRepository _billing;
    private readonly IRatesClient _rates;
    private readonly ILogger<BillingService> _logger;

    public BillingService(IBillingRepository billing, IRatesClient rates, ILogger<BillingService> logger)
    {
        _billing = billing;
        _rates = rates;
        _logger = logger;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("{Request}", RequestLogHelper.Describe("invoice.create", request));
        if (request.Amount <= 0 || request.CustomerId <= 0)
        {
            throw new RequestValidationException("Invoice amount and customer are required");
        }

        var invoice = await _billing.AddInvoiceAsync(new Invoice
        {
            CustomerId = request.CustomerId,
            OrderId = request.OrderId,
            Amount = request.Amount,
            Currency = string.IsNullOrEmpty(request.Currency) ? "USD" : request.Currency,
        }, ct);
        return Mappings.ToDto(invoice);
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(int id, CancellationToken ct = default)
    {
        var invoice = await _billing.GetInvoiceAsync(id, ct);
        return invoice is null ? null : Mappings.ToDto(invoice);
    }

    public async Task<IReadOnlyList<InvoiceDto>> ListInvoicesAsync(int customerId, CancellationToken ct = default)
    {
        var rows = await _billing.ListInvoicesAsync(customerId, ct);
        return rows.Select(Mappings.ToDto).ToList();
    }

    public async Task<PaymentDto?> PayAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var invoice = await _billing.GetInvoiceAsync(request.InvoiceId, ct);
        if (invoice is null || invoice.Status == "paid")
        {
            return null;
        }

        _logger.LogInformation("Payment for invoice {InvoiceId} with card {Card}", invoice.Id, MaskingHelper.MaskCard(request.CardNumber));
        var payment = await _billing.AddPaymentAsync(new Payment
        {
            InvoiceId = invoice.Id,
            CardNumber = request.CardNumber,
            CardHolder = request.CardHolder,
            Amount = request.Amount,
        }, ct);

        await _billing.UpdateInvoiceStatusAsync(invoice.Id, "paid", ct);
        return new PaymentDto(payment.Id, payment.InvoiceId, payment.CardNumber, payment.Amount, payment.PaidAt);
    }

    public async Task<decimal> ConvertAsync(decimal amount, string currency, CancellationToken ct = default)
    {
        var rate = await _rates.GetRateAsync(currency, ct);
        return Math.Round(amount * rate, 2);
    }
}

namespace MiddlewareDemo.Api.Requests;

public sealed record CustomerNoteRequest(string? Note);

public sealed record AuditCommentRequest(string? Target, string? Comment);

public sealed record ReportPdfRequest(string? Title);

public sealed record StockSyncRequest(string? Warehouse, int Passes);

public sealed record CatalogSummary(int Total, decimal AveragePrice, string? TopCategory);

public sealed record PartnerStatus(string Name, string Status, int Latency);

public sealed record TenantSummary(string Tenant, int Customers, DateTime GeneratedAt);

public sealed record FetchResult(string Url, int Status, string Body);

public sealed record CreateProductPayload(string? Sku, string? Name, decimal Price, string? Category, string? Description);

public sealed record InvoicePayload(int CustomerId, int OrderId, decimal Amount, string? Currency);

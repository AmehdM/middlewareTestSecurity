namespace MiddlewareDemo.Core.Models;

public sealed class Notification
{
    public int Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Channel { get; set; } = "mail";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public sealed class ExportJob
{
    public int Id { get; set; }
    public string Dataset { get; set; } = string.Empty;
    public string Format { get; set; } = "csv";
    public string Status { get; set; } = "queued";
    public string RequestedBy { get; set; } = string.Empty;
    public string? ResultPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AuditEntry
{
    public int Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime At { get; set; } = DateTime.UtcNow;
}

public sealed class FileRecord
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
}

public sealed class Tenant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

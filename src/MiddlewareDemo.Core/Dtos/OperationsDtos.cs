namespace MiddlewareDemo.Core.Dtos;

public sealed record ExportRequest(string Dataset, string Format);

public sealed record ExportRow(string Key, string Value);

public sealed record NotificationRequest(string Recipient, string Subject, string Message, string Channel);

public sealed record ReconciliationReport(int Matched, int Unmatched, List<string> Differences);

public sealed record StoredFileDto(int Id, string FileName, long Size, string ContentType);

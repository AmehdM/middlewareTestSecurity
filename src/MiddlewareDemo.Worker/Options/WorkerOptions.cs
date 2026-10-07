namespace MiddlewareDemo.Worker.Options;

public sealed class StockWorkerOptions
{
    public const string Section = "Workers:Stock";

    public int LowStockThreshold { get; set; } = 10;
    public int MaxProductId { get; set; } = 500;
    public int ReconcileEverySeconds { get; set; } = 60;
}

public sealed class ExportWorkerOptions
{
    public const string Section = "Workers:Export";

    public int PollSeconds { get; set; } = 15;
    public int BatchSize { get; set; } = 20;
    public string DigestDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "middlewaredemo-digests");
    public int DigestEveryMinutes { get; set; } = 60;
}

public sealed class SmtpSettings
{
    public string Host { get; set; } = "smtp.farmacia-demo.local";
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class SmsSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
}

public sealed class NotificationWorkerOptions
{
    public const string Section = "Workers:Notifications";

    public string Tenant { get; set; } = "default";
    public int IntervalMinutes { get; set; } = 30;
    public int PageSize { get; set; } = 50;
    public string WelcomeText { get; set; } = "your refill is ready for pickup.";
    public SmtpSettings Smtp { get; set; } = new();
    public SmsSettings Sms { get; set; } = new();
}

public sealed class FileProcessingOptions
{
    public const string Section = "Workers:Files";

    public string QueueDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "middlewaredemo-queue");
    public string OutputDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "middlewaredemo-converted");
    public string ConverterPath { get; set; } = "/usr/local/bin/docconvert";
    public int TimeoutSeconds { get; set; } = 60;
    public int PollSeconds { get; set; } = 5;
}

public sealed class AuditPurgeOptions
{
    public const string Section = "Workers:Audit";

    public int RetentionDays { get; set; } = 365;
    public int PageSize { get; set; } = 200;
    public int IntervalHours { get; set; } = 24;
    public string ArchiveDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "middlewaredemo-audit");
}

public sealed class RatesOptions
{
    public const string Section = "Workers:Rates";

    public string[] Currencies { get; set; } = { "USD", "EUR", "HNL" };
    public int RefreshMinutes { get; set; } = 30;
}

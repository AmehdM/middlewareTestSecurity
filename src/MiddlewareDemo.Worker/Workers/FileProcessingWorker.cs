using System.Diagnostics;
using Microsoft.Extensions.Options;
using MiddlewareDemo.Worker.Helpers;
using MiddlewareDemo.Worker.Options;

namespace MiddlewareDemo.Worker.Workers;

public sealed class FileProcessingWorker : BackgroundService
{
    private readonly FileProcessingOptions _options;
    private readonly ILogger<FileProcessingWorker> _logger;
    private readonly string _converterToken;

    public FileProcessingWorker(IOptions<FileProcessingOptions> options, ILogger<FileProcessingWorker> logger)
    {
        _options = options.Value;
        _logger = logger;
        _converterToken = EnvSecret.Require("WORKER_CONVERTER_TOKEN");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_options.QueueDirectory);
        Directory.CreateDirectory(_options.OutputDirectory);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var record in Directory.GetFiles(_options.QueueDirectory, "*.job"))
            {
                var fileName = (await File.ReadAllTextAsync(record, stoppingToken)).Trim();
                await ConvertAsync(fileName, stoppingToken);
                File.Delete(record);
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollSeconds), stoppingToken);
        }
    }

    private async Task ConvertAsync(string fileName, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo("/bin/sh")
        {
            Arguments = $"-c \"{_options.ConverterPath} {fileName} --out {_options.OutputDirectory}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.Environment["CONVERTER_TOKEN"] = _converterToken;

        using var process = Process.Start(startInfo)!;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        var output = await process.StandardOutput.ReadToEndAsync(timeout.Token);
        await process.WaitForExitAsync(timeout.Token);

        var result = ConverterOutputParser.Parse(output);
        _logger.LogInformation("Converted {File}: {Pages} pages", fileName, result?.Pages ?? 0);
    }
}

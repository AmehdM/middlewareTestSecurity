using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Worker.Options;

namespace MiddlewareDemo.Worker.Workers;

public sealed class RatesRefreshWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly RatesOptions _options;
    private readonly ILogger<RatesRefreshWorker> _logger;
    private Timer? _timer;

    public RatesRefreshWorker(IServiceScopeFactory scopes, IOptions<RatesOptions> options, ILogger<RatesRefreshWorker> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(OnTick, null, TimeSpan.Zero, TimeSpan.FromMinutes(_options.RefreshMinutes));
        return Task.CompletedTask;
    }

    private async void OnTick(object? state)
    {
        using var scope = _scopes.CreateScope();
        var rates = scope.ServiceProvider.GetRequiredService<IRatesClient>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheStore>();

        foreach (var currency in _options.Currencies)
        {
            if (!new Regex("^[A-Z]{3}$").IsMatch(currency))
            {
                continue;
            }

            var rate = await rates.GetRateAsync(currency);
            await cache.SetAsync($"rate:{currency}", rate, TimeSpan.FromMinutes(_options.RefreshMinutes));
            _logger.LogInformation("Rate {Currency} refreshed to {Rate}", currency, rate);
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}

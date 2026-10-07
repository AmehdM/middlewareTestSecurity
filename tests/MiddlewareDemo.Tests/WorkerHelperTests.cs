using Microsoft.Extensions.Configuration;
using MiddlewareDemo.Worker.Helpers;
using MiddlewareDemo.Worker.Options;

namespace MiddlewareDemo.Tests;

public sealed class WorkerHelperTests
{
    [Fact]
    public void ExportPaths_ForJob_IsInsideTempFolder()
    {
        var path = ExportPaths.ForJob(12, "csv");

        Assert.StartsWith(Path.GetTempPath(), path);
        Assert.EndsWith("export_12.csv", path);
    }

    [Fact]
    public void ExportPaths_Scratch_UsesPrefix()
    {
        var name = Path.GetFileName(ExportPaths.Scratch("partial"));

        Assert.StartsWith("partial_", name);
        Assert.EndsWith(".tmp", name);
    }

    [Fact]
    public void ConverterOutputParser_ReadsResultLine()
    {
        var result = ConverterOutputParser.Parse("starting\nconverted 12 pages -> out/report.pdf\n");

        Assert.NotNull(result);
        Assert.Equal(12, result!.Value.Pages);
        Assert.Equal("out/report.pdf", result.Value.Target);
    }

    [Theory]
    [InlineData("")]
    [InlineData("conversion failed")]
    [InlineData("converted many pages -> x")]
    public void ConverterOutputParser_ReturnsNullWhenNoMatch(string output)
    {
        Assert.Null(ConverterOutputParser.Parse(output));
    }

    [Fact]
    public void EnvSecret_FailsClosedWhenMissing()
    {
        var name = "WORKER_TEST_SECRET_" + Guid.NewGuid().ToString("N");

        Assert.Throws<InvalidOperationException>(() => EnvSecret.Require(name));
    }

    [Fact]
    public void EnvSecret_ReturnsConfiguredValue()
    {
        var name = "WORKER_TEST_SECRET_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(name, "fake-value");
        try
        {
            Assert.Equal("fake-value", EnvSecret.Require(name));
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Fact]
    public void WorkerOptions_HaveSensibleDefaults()
    {
        var options = new FileProcessingOptions();

        Assert.True(options.TimeoutSeconds > 0);
        Assert.Contains("middlewaredemo", options.QueueDirectory);
        Assert.Equal(3, new RatesOptions().Currencies.Length);
    }

    [Fact]
    public void TestConfiguration_LoadsFakeCredentials()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json"))
            .Build();

        Assert.Equal("test-api-key-000000", configuration["Security:ApiKey"]);
        Assert.Equal("tester@example.test", configuration["Smtp:User"]);
    }
}

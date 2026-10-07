using MiddlewareDemo.Core.Logging;
using MiddlewareDemo.Core.Security;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Tests;

public sealed class SecurityHelperTests
{
    [Fact]
    public void Hash_ProducesVersionedFormat()
    {
        var hash = PasswordHasher.Hash("Sup3r-Secret!");

        var parts = hash.Split('.');
        Assert.Equal(4, parts.Length);
        Assert.Equal("v1", parts[0]);
        Assert.Equal("100000", parts[1]);
    }

    [Fact]
    public void Hash_UsesDifferentSaltEachTime()
    {
        Assert.NotEqual(PasswordHasher.Hash("same-password"), PasswordHasher.Hash("same-password"));
    }

    [Fact]
    public void Verify_AcceptsCorrectPassword()
    {
        var hash = PasswordHasher.Hash("correct horse");

        Assert.True(PasswordHasher.Verify(hash, "correct horse"));
    }

    [Theory]
    [InlineData("wrong horse")]
    [InlineData("")]
    public void Verify_RejectsOtherPasswords(string attempt)
    {
        var hash = PasswordHasher.Hash("correct horse");

        Assert.False(PasswordHasher.Verify(hash, attempt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("v2.1.abc.def")]
    [InlineData("not-a-hash")]
    public void Verify_RejectsMalformedHashes(string stored)
    {
        Assert.False(PasswordHasher.Verify(stored, "anything"));
    }

    [Fact]
    public void PasswordService_UsesPolicyAndHasher()
    {
        var service = new PasswordService();

        Assert.Empty(service.Validate("long-enough-password"));
        Assert.NotEmpty(service.Validate("abc"));
        Assert.True(service.Verify(service.Hash("long-enough-password"), "long-enough-password"));
    }

    [Theory]
    [InlineData("4111111111111111", "************1111")]
    [InlineData("4111-1111-1111-1234", "************1234")]
    [InlineData("1234", "****")]
    [InlineData(null, "****")]
    public void MaskCard_KeepsOnlyLastFourDigits(string? pan, string expected)
    {
        Assert.Equal(expected, MaskingHelper.MaskCard(pan));
    }

    [Theory]
    [InlineData("0801-1990-12345", "***********2345")]
    [InlineData("123", "****")]
    [InlineData("", "****")]
    public void MaskNationalId_HidesLeadingCharacters(string value, string expected)
    {
        Assert.Equal(expected, MaskingHelper.MaskNationalId(value));
    }

    [Fact]
    public void ConstantTime_ComparesValues()
    {
        Assert.True(ConstantTime.AreEqual("abc123", "abc123"));
        Assert.False(ConstantTime.AreEqual("abc123", "abc124"));
        Assert.False(ConstantTime.AreEqual("abc", "abcd"));
    }

    [Fact]
    public void RequestLogHelper_SerializesOperationAndPayload()
    {
        var text = RequestLogHelper.Describe("inventory.reserve", new { productId = 4, quantity = 2 });

        Assert.StartsWith("inventory.reserve: ", text);
        Assert.Contains("\"productId\":4", text);
    }
}

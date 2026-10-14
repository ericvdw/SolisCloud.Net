// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Cli.Tests;

public sealed class CredentialsTests
{
    [Fact]
    public void Loads_the_id_secret_and_default_server()
    {
        bool loaded = Credentials.TryLoad(
            Environment(id: " 1300386381676644416 ", secret: "secret"),
            out SolisCloudClientOptions? options,
            out string? error);

        Assert.True(loaded);
        Assert.Null(error);
        Assert.Equal("1300386381676644416", options!.ApiId);
        Assert.Equal("secret", options.ApiSecret);
        Assert.Equal(SolisCloudClientOptions.DefaultBaseAddress, options.BaseAddress);
    }

    [Fact]
    public void Names_every_missing_variable()
    {
        bool loaded = Credentials.TryLoad(Environment(id: null, secret: " "), out _, out string? error);

        Assert.False(loaded);
        Assert.Contains(Credentials.ApiIdVariable, error, StringComparison.Ordinal);
        Assert.Contains(Credentials.ApiSecretVariable, error, StringComparison.Ordinal);
    }

    [Fact]
    public void Accepts_a_custom_server()
    {
        bool loaded = Credentials.TryLoad(
            Environment(id: "id", secret: "secret", baseAddress: "https://test.soliscloud.com:3333/"),
            out SolisCloudClientOptions? options,
            out _);

        Assert.True(loaded);
        Assert.Equal(new Uri("https://test.soliscloud.com:3333/"), options!.BaseAddress);
    }

    [Fact]
    public void Rejects_a_relative_server()
    {
        bool loaded = Credentials.TryLoad(Environment(id: "id", secret: "secret", baseAddress: "soliscloud"), out _, out string? error);

        Assert.False(loaded);
        Assert.Contains(Credentials.BaseAddressVariable, error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1300386381676644416", "1300…4416")]
    [InlineData("12345678", "********")]
    [InlineData("", "")]
    public void Mask_hides_the_middle_of_the_id(string apiId, string expected) => Assert.Equal(expected, Credentials.Mask(apiId));

    private static Func<string, string?> Environment(string? id, string? secret, string? baseAddress = null) =>
        name => name switch
        {
            Credentials.ApiIdVariable => id,
            Credentials.ApiSecretVariable => secret,
            Credentials.BaseAddressVariable => baseAddress,
            _ => null,
        };
}

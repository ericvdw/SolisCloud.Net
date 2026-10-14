// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text;
using SolisCloud.Cli.Sanitizing;

namespace SolisCloud.Cli.Tests;

public sealed class PseudonymizerTests
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("test-key");

    [Fact]
    public void The_same_value_always_gets_the_same_fake()
    {
        Pseudonymizer pseudonymizer = new(Key);

        Assert.Equal(pseudonymizer.Pseudonymize("120B40198150131"), pseudonymizer.Pseudonymize("120B40198150131"));
        Assert.Equal(new Pseudonymizer(Key).Pseudonymize("120B40198150131"), pseudonymizer.Pseudonymize("120B40198150131"));
    }

    [Theory]
    [InlineData("1298491919448631809")]
    [InlineData("120B40198150131")]
    [InlineData("00FFC0011557001")]
    [InlineData("abc-DEF.123")]
    [InlineData("a-much-longer-value-than-the-thirty-two-byte-hash-0123456789")]
    public void The_fake_keeps_the_shape_of_the_value(string value)
    {
        string fake = new Pseudonymizer(Key).Pseudonymize(value);

        Assert.NotEqual(value, fake, StringComparer.Ordinal);
        Assert.Equal(value.Length, fake.Length);
        for (int i = 0; i < value.Length; i++)
        {
            Assert.Equal(Shape(value[i]), Shape(fake[i]));
        }
    }

    [Fact]
    public void A_leading_non_zero_digit_stays_non_zero()
    {
        Pseudonymizer pseudonymizer = new(Key);
        for (int i = 1; i < 200; i++)
        {
            Assert.NotEqual('0', pseudonymizer.Pseudonymize("9" + i.ToString(System.Globalization.CultureInfo.InvariantCulture))[0]);
        }
    }

    [Fact]
    public void Different_keys_give_different_fakes()
    {
        string first = new Pseudonymizer(Key).Pseudonymize("1298491919448631809");
        string second = new Pseudonymizer(Encoding.UTF8.GetBytes("another-key")).Pseudonymize("1298491919448631809");

        Assert.NotEqual(first, second, StringComparer.Ordinal);
    }

    [Fact]
    public void An_empty_value_stays_empty()
    {
        Assert.Equal(string.Empty, new Pseudonymizer(Key).Pseudonymize(string.Empty));
    }

    [Fact]
    public void An_empty_key_is_rejected()
    {
        _ = Assert.Throws<ArgumentException>(() => new Pseudonymizer([]));
    }

    private static string Shape(char c) =>
        char.IsAsciiDigit(c) ? "digit"
        : char.IsAsciiLetterUpper(c) ? "upper"
        : char.IsAsciiLetterLower(c) ? "lower"
        : c.ToString();
}

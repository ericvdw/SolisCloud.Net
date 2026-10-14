// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using System.Text;

namespace SolisCloud.Tests;

public sealed class SolisRequestSignerTests
{
    [Fact]
    public void ComputeContentMd5_matches_the_api_document_example()
    {
        // API document section 2.5: the Content-MD5 of {"pageNo":1,"pageSize":10}.
        byte[] body = Encoding.UTF8.GetBytes("{\"pageNo\":1,\"pageSize\":10}");

        string md5 = SolisRequestSigner.ComputeContentMd5(body);

        Assert.Equal("kxdxk7rbAsrzSIWgEwhH4w==", md5);
    }

    [Fact]
    public void ComputeContentMd5_of_an_empty_body_is_the_empty_digest()
    {
        Assert.Equal("1B2M2Y8AsgTpgAmY7PhCfg==", SolisRequestSigner.ComputeContentMd5([]));
    }

    [Theory]
    [InlineData(2019, 7, 26, 6, 0, 46, "Fri, 26 Jul 2019 06:00:46 GMT")]
    [InlineData(2023, 6, 6, 18, 5, 9, "Tue, 06 Jun 2023 18:05:09 GMT")]
    public void FormatDate_produces_rfc1123_with_two_digit_day(int year, int month, int day, int hour, int minute, int second, string expected)
    {
        DateTimeOffset timestamp = new(year, month, day, hour, minute, second, TimeSpan.Zero);

        Assert.Equal(expected, SolisRequestSigner.FormatDate(timestamp));
    }

    [Fact]
    public void FormatDate_converts_to_utc()
    {
        DateTimeOffset southAfrica = new(2019, 7, 26, 8, 0, 46, TimeSpan.FromHours(2));

        Assert.Equal("Fri, 26 Jul 2019 06:00:46 GMT", SolisRequestSigner.FormatDate(southAfrica));
    }

    [Theory]
    [InlineData("af-ZA")]
    [InlineData("ja-JP")]
    [InlineData("ar-SA")]
    public void FormatDate_ignores_the_current_culture(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);

            string date = SolisRequestSigner.FormatDate(new DateTimeOffset(2019, 7, 26, 6, 0, 46, TimeSpan.Zero));

            Assert.Equal("Fri, 26 Jul 2019 06:00:46 GMT", date);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void BuildStringToSign_joins_the_parts_with_newlines()
    {
        string stringToSign = SolisRequestSigner.BuildStringToSign(
            "kxdxk7rbAsrzSIWgEwhH4w==",
            "application/json",
            "Fri, 26 Jul 2019 06:00:46 GMT",
            "/v1/api/userStationList");

        Assert.Equal(
            "POST\nkxdxk7rbAsrzSIWgEwhH4w==\napplication/json\nFri, 26 Jul 2019 06:00:46 GMT\n/v1/api/userStationList",
            stringToSign);
    }

    [Fact]
    public void ComputeSignature_matches_an_independent_hmac_sha1()
    {
        // Reference value computed outside .NET:
        // printf 'POST\n...\n/v1/api/userStationList' | openssl dgst -sha1 -hmac 'test-secret' -binary | base64
        const string StringToSign =
            "POST\nkxdxk7rbAsrzSIWgEwhH4w==\napplication/json\nFri, 26 Jul 2019 06:00:46 GMT\n/v1/api/userStationList";

        string signature = SolisRequestSigner.ComputeSignature("test-secret", StringToSign);

        Assert.Equal("/GOIW8erQaUwWozVKxeKlb/FNkA=", signature);
    }

    [Fact]
    public void FormatAuthorization_uses_the_api_scheme()
    {
        Assert.Equal("API 1300386381676644416:abc=", SolisRequestSigner.FormatAuthorization("1300386381676644416", "abc="));
    }

    [Fact]
    public void Null_arguments_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => SolisRequestSigner.ComputeContentMd5(null!));
        Assert.Throws<ArgumentNullException>(() => SolisRequestSigner.ComputeSignature(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => SolisRequestSigner.BuildStringToSign("a", "b", "c", null!));
        Assert.Throws<ArgumentNullException>(() => SolisRequestSigner.FormatAuthorization("id", null!));
    }
}

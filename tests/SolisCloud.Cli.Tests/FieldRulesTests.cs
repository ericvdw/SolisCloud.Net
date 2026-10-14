// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using SolisCloud.Cli.Sanitizing;

namespace SolisCloud.Cli.Tests;

public sealed class FieldRulesTests
{
    [Theory]
    [InlineData("id")]
    [InlineData("sn")]
    [InlineData("stationId")]
    [InlineData("userId")]
    [InlineData("installerId")]
    [InlineData("collectorSn")]
    [InlineData("alarmDeviceSn")]
    [InlineData("snList")]
    [InlineData("idList")]
    [InlineData("nmiCode")]
    [InlineData("sno")]
    [InlineData("city")]
    [InlineData("someUndocumentedId")]
    public void Identifiers(string name) => Assert.Equal(FieldKind.Identifier, FieldRules.Classify(name));

    [Theory]
    [InlineData("stationName")]
    [InlineData("name")]
    [InlineData("addr")]
    [InlineData("installer")]
    [InlineData("installerMobile")]
    [InlineData("installerEmail")]
    [InlineData("phone")]
    [InlineData("cityStr")]
    [InlineData("pic1Url")]
    [InlineData("remark2")]
    public void Personal_text(string name) => Assert.Equal(FieldKind.PersonalText, FieldRules.Classify(name));

    [Theory]
    [InlineData("latitude")]
    [InlineData("longitude")]
    [InlineData("lat")]
    public void Locations(string name) => Assert.Equal(FieldKind.Location, FieldRules.Classify(name));

    [Theory]
    [InlineData("pac")]
    [InlineData("eToday")]
    [InlineData("state")]
    [InlineData("money")]
    [InlineData("timeZone")]
    [InlineData("timeZoneId")]
    [InlineData("timeZoneName")]
    [InlineData("rs485ComAddr")]
    [InlineData("alarmCode")]
    [InlineData("dataTimestamp")]
    [InlineData("batteryCapacitySoc")]
    public void Measurements_and_settings_are_kept(string name) => Assert.Equal(FieldKind.Keep, FieldRules.Classify(name));
}

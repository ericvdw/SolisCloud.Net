// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Cli.Tests.TestSupport;

/// <summary>
/// Response payloads shaped like the API document's examples, with made-up personal data.
/// </summary>
internal static class SampleData
{
    public const string StationId = "1298491919448631809";
    public const string StationName = "Van Wyk Family Home";
    public const string Address = "12 Protea Street, Stellenbosch";
    public const string InverterId = "1308675217944611083";
    public const string InverterSn = "120B40198150131";
    public const string CollectorId = "1306858901386141423";
    public const string CollectorSn = "404314859";

    public static string StationList { get; } = """
        {
          "page": {
            "current": 1,
            "total": 1,
            "records": [
              {
                "id": "1298491919448631809",
                "stationName": "Van Wyk Family Home",
                "addr": "12 Protea Street, Stellenbosch",
                "userId": 1300386381676644416,
                "installerMobile": "+27 82 555 0123",
                "latitude": "-33.9321",
                "longitude": 18.8602,
                "capacity": 8.2,
                "capacityStr": "kWp",
                "money": "ZAR",
                "timeZone": 2.00,
                "timeZoneId": "115",
                "state": 1
              }
            ]
          }
        }
        """;

    // Shaped like a real station detail: the street sits in "addrOrigin", and "extraInfo" is a JSON string that
    // names the inverter by serial number before any inverter response has been seen.
    public static string StationDetail { get; } = """
        {
          "id": "1298491919448631809",
          "stationName": "Van Wyk Family Home",
          "addrOrigin": "Protea Street 12",
          "buildAddr": "12 Protea Street,Stellenbosch,Western Cape,South Africa",
          "weather": "Sunrise 05:33 Sunset 18:11, Stellenbosch Local Municipality, Clouds",
          "timeZoneStandardId": "Africa/Johannesburg",
          "timeZoneName": "(UTC+02:00) Africa/Johannesburg",
          "batteryTypeName": "Pylontech US3000C",
          "extraInfo": "{\"invs\":[{\"sn\":\"120B40198150131\",\"m\":\"3126\"}],\"inverterSn\":\"120B40198150131\",\"epVersion\":\"v5\"}"
        }
        """;

    public static string InverterList { get; } = """
        {
          "page": {
            "records": [
              {
                "id": "1308675217944611083",
                "sn": "120B40198150131",
                "stationId": "1298491919448631809",
                "stationName": "Van Wyk Family Home",
                "collectorSn": "404314859",
                "pac": 5.025,
                "pacStr": "kW"
              }
            ]
          }
        }
        """;

    public static string CollectorList { get; } = """
        {
          "page": {
            "records": [
              {
                "id": "1306858901386141423",
                "sn": "404314859",
                "stationId": "1298491919448631809"
              }
            ]
          }
        }
        """;
}

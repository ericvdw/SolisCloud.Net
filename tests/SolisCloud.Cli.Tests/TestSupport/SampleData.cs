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

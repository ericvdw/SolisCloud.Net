# SolisCloud.Net

An unofficial, strongly typed .NET client for the [SolisCloud](https://www.soliscloud.com) platform API (v2.0.3).

> **Not affiliated with Ginlong (Solis) Technologies.** "Solis" and "SolisCloud" are trademarks of their respective owners.
> This project talks to the public SolisCloud API using credentials you obtain from your own SolisCloud account.

## Status

Early development. The public API will change until 1.0.

## Goals

- Correct, tested request signing (HMAC-SHA1 / Content-MD5) — the part everyone gets subtly wrong.
- Normalised models: consistent units (`Power`, `Energy`), arrays instead of `uPv1…uPv32`, enums instead of magic numbers, `DateTimeOffset` instead of epoch strings.
- Raw access to every response, so undocumented fields are never lost.
- Respect for the platform's rate limits (2 requests/second per endpoint).
- Runs anywhere: `netstandard2.0` and `net10.0`, trimming- and AOT-friendly on modern .NET.

## Getting API credentials

Log in to SolisCloud and go to **Account → Basic Settings → API Management**. API access may first need to be enabled by Solis support.
Never commit your API ID or secret.

## The `solis` command-line tool

The CLI reads credentials from environment variables, never from the command line (which would leave them in shell history):

```powershell
$env:SOLIS_API_ID = "your API ID"
$env:SOLIS_API_SECRET = "your API secret"
# Optional: $env:SOLIS_BASE_ADDRESS = "https://www.soliscloud.com:13333/"
```

Run it from the repository with `dotnet run --project src/SolisCloud.Cli --`, followed by one of these commands:

| Command | What it does |
|---|---|
| `whoami` | Checks the credentials and the computer's clock (requests fail beyond ±15 minutes) and lists the stations the account can see. |
| `raw <operation> [--body <json>] [--envelope]` | Sends any operation, e.g. `raw inverterDetail --body '{"sn":"..."}'`, and prints the response. Operations that change account data also need `--allow-write`. |
| `capture [--output <folder>] [--date yyyy-MM-dd] [--no-sanitize]` | Calls every read operation for everything the account can see and saves each request and response under `captures/<timestamp>/`. |

### Sharing captures

The API document doesn't fully describe what the API returns, and responses differ between inverter models. Captures from real systems are how this library's models get written and tested, so they're the most useful contribution you can make.

`capture` sanitises by default:
- IDs and serial numbers become consistent fakes of the same shape, so an inverter's `stationId` still matches its station.
- Names, addresses, contact details and URLs become `REDACTED`.
- Coordinates become `0`.

Sanitising is best-effort, so **read the files before sharing them**. `captures/` is in `.gitignore`.

## Repository layout

| Path | What |
|------|------|
| `src/SolisCloud.Client` | The library (NuGet package). |
| `src/SolisCloud.Cli` | `solis` command-line tool for checking credentials and capturing sanitised responses. |
| `tests/` | Unit tests for both, and recorded fixtures. |
| `docs/` | Design notes, API quirks, and the analyzer guide. |

## Building

Requires the .NET 10 SDK.

```powershell
./build.ps1          # restore, build (warnings are errors), test; logs go to artifacts/logs
```

## Licence

[MIT](LICENSE)

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

## Repository layout

| Path | What |
|------|------|
| `src/SolisCloud.Client` | The library (NuGet package). |
| `src/SolisCloud.Cli` | `solis` command-line tool for checking credentials and capturing sanitised responses. |
| `tests/` | Unit tests and recorded fixtures. |
| `docs/` | Design notes, API quirks, and the analyzer guide. |

## Building

Requires the .NET 10 SDK.

```powershell
./build.ps1          # restore, build (warnings are errors), test; logs go to artifacts/logs
```

## Licence

[MIT](LICENSE)

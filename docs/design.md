# Design

## Scope

This repository is the **open-source SolisCloud client**: a .NET library and a small CLI. Applications built on it (pollers, databases, MQTT/Home Assistant bridges, dashboards) live elsewhere and use the published package. That keeps the library honest: anything an application needs must be available through its public API.

## Layers

1. **Transport.** `SolisCloudClient.SendAsync(endpoint, body)` signs, rate-limits and sends a request, checks the response envelope, and returns the `data` payload as raw JSON (`SolisCloudResponse`). Every operation is reachable this way, including ones not yet modelled.
2. **Typed operations** (next). Methods such as `GetInverterDetailAsync(sn)` that build request bodies and map responses to models. Paged operations return `IAsyncEnumerable<T>`.
3. **Models** (next). Two levels per response:
   - raw DTOs that mirror the wire format and keep unknown fields (`[JsonExtensionData]`), and
   - normalised models with fixed units (`Power`, `Energy` value types), arrays instead of numbered fields, enums instead of magic numbers, and `DateTimeOffset` instead of epoch strings.

   Models are written from **captured real responses** (`solis capture`), not from the document's tables alone. See [api-notes.md](api-notes.md) for why.

## Decisions

| Decision | Reason |
|---|---|
| Targets `netstandard2.0` and `net10.0` | Reaches .NET Framework 4.6.2+, Mono and Unity as well as modern .NET; the net10.0 build adds AOT/trimming metadata and faster APIs. |
| No dependency injection or Polly in the core package | Keeps the dependency graph minimal. The client accepts any `HttpClient`, so retries and logging can be added in its handler pipeline. A separate `SolisCloud.Client.Extensions` package can add `IHttpClientFactory` wiring later. |
| Own rate limiter instead of `System.Threading.RateLimiting` | That package isn't in-box even on .NET 10. The limiter is ~100 lines and takes `TimeProvider`, so tests run without real delays. |
| `TimeProvider` everywhere time matters | Signing dates and rate limiting are testable with a fake clock. `DateTime.Now` and friends are banned (see `BannedSymbols.txt`). |
| Write operations opt-in | Station administration changes account data; a monitoring app should never send one by accident. |
| Raw access always available | The document is incomplete; users must never be blocked by a missing model. |
| No `packages.lock.json` | Central Package Management pins every direct dependency, plus any transitive one listed there (transitive pinning). Lock files broke locked-mode CI: `IsAotCompatible` pulls in `Microsoft.NET.ILLink.Tasks` at the version of whichever SDK patch is installed, and the test project adds `net48` only on Windows, so each machine produced a different lock file. |
| Tests run on Microsoft.Testing.Platform | xUnit v3 test projects are executables. `global.json` opts `dotnet test` into the new platform (the VSTest bridge is unsupported on the .NET 10 SDK), so `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` aren't needed. The test project can also be run directly. |
| CLI credentials from environment variables only | Command-line options end up in shell history and process listings. |
| CLI commands don't depend on System.CommandLine | `CliCommands.cs` only parses arguments; each command is a plain class taking a `CliContext` (output, environment, clock, client factory), so tests run it directly with a fake HTTP handler. |
| `capture` sanitises by default, with format-preserving fakes | Captures are meant to be shared as fixtures. Fakes keep the shape (digits, letters, length) of the original and are consistent within a capture, so models can be tested against realistic IDs and relationships between responses survive. Each capture uses a random key, so fakes can't be correlated between captures. |

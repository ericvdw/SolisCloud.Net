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

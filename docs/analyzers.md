# Analyzers and code quality

This repository builds with **warnings as errors** and close to every analyzer .NET offers turned on. This page explains what each layer does, how it is configured, and how to work with it day to day. It also serves as a guide to .NET analyzers in general.

## The layers

| Layer | Rule prefix | Comes from | Turned on by |
|---|---|---|---|
| C# compiler | `CS` | the compiler | always on; `<Nullable>enable</Nullable>` and `<Features>strict</Features>` add more |
| .NET code-quality analyzers | `CA` | the .NET SDK (no package) | `<AnalysisLevel>latest-all</AnalysisLevel>` |
| Code-style analyzers | `IDE` | the .NET SDK (no package) | `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` + severities in `.editorconfig` |
| Trimming / AOT analyzers | `IL` | the .NET SDK | `<IsAotCompatible>true</IsAotCompatible>` (library, net10.0 only) |
| Meziantou.Analyzer | `MA` | NuGet | `GlobalPackageReference` in `Directory.Packages.props` |
| Banned API analyzer | `RS0030` | NuGet (Microsoft.CodeAnalysis.BannedApiAnalyzers) | `GlobalPackageReference` + `BannedSymbols.txt` |
| Public API analyzer | `RS0016`, `RS0017`, … | NuGet (Microsoft.CodeAnalysis.PublicApiAnalyzers) | library `.csproj` + `PublicAPI.*.txt` |
| xUnit analyzers | `xUnit` | ships inside `xunit.v3` | automatic in test projects |

`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props` turns every warning from every layer into a build failure.

### 1. Compiler (`CS`)

Nullable reference types (`<Nullable>enable</Nullable>`) are the most valuable compiler feature to enable: `string` means "never null" and `string?` means "may be null", and the compiler tracks this through the code. `<Features>strict</Features>` turns on a handful of extra diagnostics that are off by default only for backwards compatibility.

### 2. .NET code-quality analyzers (`CA`)

These ship with the SDK. `AnalysisLevel` has two parts, **version** and **mode**:

- **Version** (`latest`, `9`, `10`, …) chooses which rule set. `latest` picks up new rules when the SDK is upgraded, which can break the build on an SDK bump. That's deliberate here: fix new findings when they appear. Pin a number (e.g. `10-all`) if you'd rather upgrade on your own schedule.
- **Mode** (`none`, `default`, `minimum`, `recommended`, `all`) chooses how many rules are enabled as warnings. `all` enables all of them, so a few are switched off again in `.editorconfig` with a reason next to each.

Rules worth understanding because they shaped this code:

| Rule | What it caught | Where to look |
|---|---|---|
| CA2000 | An object initializer (`new SocketsHttpHandler { … }`) can throw *after* the object is created, leaking it. Properties are now set inside the `try`. | `SolisCloudClient.CreateOwnedHttpClient` |
| CA2007 | Library code must `ConfigureAwait(false)` so it never captures a UI/ASP.NET synchronisation context, which can deadlock callers that block. Off in the CLI and tests, where it doesn't apply. | every `await` in `src/SolisCloud.Client` |
| CA1062 | Public methods must validate arguments: callers may not use nullable annotations. `Guard.NotNull` does it; `[ValidatedNotNull]` tells the analyzer so. | `Internal/Guard.cs` |
| CA1305 / CA1307 / CA1310 | Formatting and string comparison must state culture and comparison explicitly. A `Date` header formatted in the user's culture would break the signature on, say, an Arabic-locale PC. | `SolisRequestSigner.FormatDate` + `FormatDate_ignores_the_current_culture` test |
| CA1513 | Use `ObjectDisposedException.ThrowIf` on modern .NET. It doesn't exist on netstandard2.0, so `Guard.NotDisposed` picks the right form per target. | `Internal/Guard.cs` |
| CA5350 / CA5351 | MD5 and SHA-1 are weak. The protocol requires them, so they're suppressed **at the method**, with a justification. | `SolisRequestSigner` |
| CA1032 | Exceptions need the three standard constructors. | `SolisCloud*Exception.cs` |

Each rule has a docs page at `https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/<id>`, e.g. `.../ca2000`.

### 3. Code style (`IDE`)

These are the rules behind the editor's light-bulb suggestions. By default they only show in the IDE. `EnforceCodeStyleInBuild` makes `dotnet build` (and CI) run them too, **but only for rules whose severity is `warning` or `error`**. That's why `.editorconfig` lists `dotnet_diagnostic.IDExxxx.severity = warning` for each enforced rule.

Two of them need extra setup:

- **IDE0005** (unnecessary `using`) only runs on build when `GenerateDocumentationFile` is on, a long-standing quirk. It's on anyway, because public APIs need XML docs (`CS1591`).
- **IDE0073** (file header) checks the copyright line against `file_header_template`.

Style *preferences* (`csharp_style_var_elsewhere = false:warning`) and the rule that enforces them (IDE0008) work together: the preference says what's right, and the severity on the preference or the rule says how loudly to complain.

### 4. Trimming and Native AOT (`IL`)

`IsAotCompatible` on the net10.0 build enables the trim, AOT and single-file analyzers. They flag reflection-based code (for example `JsonSerializer.Serialize(object)`) that can't work once unused code is trimmed away. The library uses `JsonDocument` and, later, source-generated JSON, so it stays usable in AOT apps such as a small Raspberry Pi service.

### 5. Meziantou.Analyzer (`MA`)

A large, well-maintained third-party set that fills gaps in the built-in rules. Examples that matter here:

- MA0040: forward the `CancellationToken` you were given.
- MA0048: file name must match the type name.
- MA0051: method too long.
- MA0009: regex without a timeout.

Where a Meziantou rule duplicates a CA rule we already enforce, the MA copy is switched off in `.editorconfig` so each problem is reported once.

### 6. Banned APIs (`RS0030`)

`BannedSymbols.txt` lists APIs nobody may call, each with a message saying what to use instead. Here it bans `DateTime.Now`/`UtcNow` (use the injected `TimeProvider`, so tests control time), blocking on tasks (`.Result`, `.Wait()`), and Newtonsoft.Json. The format is the [documentation comment ID](https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/#id-strings): `P:` property, `M:` method, `T:` type, `N:` namespace.

### 7. Public API tracking (`RS0016`, `RS0017`)

Every public type and member of the library must be listed in `PublicAPI.Unshipped.txt`. Adding, removing or changing a public member without updating the file fails the build. This makes every API change visible in code review, which matters for a library other people depend on.

Workflow:

1. Add or change a public member and build. `RS0016` names the member that isn't listed.
2. Run `./build.ps1 -UpdatePublicApi`, which applies the analyzer's own code fix through `dotnet format`. Or use the light bulb in the IDE.
3. Review the diff to `PublicAPI.Unshipped.txt`.
4. On release, move the lines from `Unshipped` to `Shipped`. From then on, removing them is a breaking change and `RS0017` will say so.

### 8. xUnit analyzers

They ship inside `xunit.v3`. For example, xUnit1051 requires passing `TestContext.Current.CancellationToken` to async calls, so a cancelled test run stops promptly, and xUnit1030 forbids `ConfigureAwait(false)` in tests.

## Findings that changed the code

The rule tables above say what each layer does. These are real findings from this repository, kept as worked examples:

| Rule | Finding | Fix |
|---|---|---|
| RS0026 | `SendAsync` had two public overloads (`string` and `ReadOnlyMemory<byte>` body) that **both** had an optional `CancellationToken`. Adding another optional parameter to either later is a binary break, and callers can hit ambiguous overloads. | Kept one overload, the bytes one: the signature covers the exact bytes sent, so bytes are the honest input. Tests use a small `SendAsync(string)` extension in `TestSupport`. |
| MA0158 | Locking on a plain `object`. .NET 9 added `System.Threading.Lock`, a dedicated type that's faster and can't be locked by accident through some other reference. | `Lock` on `NET9_0_OR_GREATER`, `object` on netstandard2.0, chosen with `#if`. The `lock` statement is unchanged. |
| MA0002 | `Assert.Contains("Date: …", lines)` compares strings without saying how. | Pass `StringComparer.Ordinal`: wire bytes must match exactly. |
| IDE0007 | `using (MD5 md5 = MD5.Create())`: the type is obvious from the right-hand side, and `.editorconfig` asks for `var` exactly then. | `using (var md5 = MD5.Create())`. Only the netstandard2.0 branch had this, which is why it only failed for that target. |

Multi-targeting matters for analyzers: each target framework compiles separately, so a finding can appear for one target only (IDE0007 above, or MA0158, which only fires where `Lock` exists). Read the `[...::TargetFramework=...]` suffix in the error.

## Configuring rules

All configuration lives in `.editorconfig`:

```ini
dotnet_diagnostic.CA1303.severity = none          # turn a rule off everywhere
[tests/**.cs]
dotnet_diagnostic.CA1707.severity = none          # ...or only for some files
dotnet_code_quality.dispose_ownership_transfer_at_constructor = true   # rule options
```

Severities: `error`, `warning`, `suggestion` (IDE only), `silent` (fix offered, no squiggle), `none`.

### Suppressing a single finding

When a rule is right in general but wrong in one place, suppress it **as narrowly as possible, with a reason**:

```csharp
[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms",
    Justification = "MD5 is mandated by the SolisCloud protocol as a body checksum.")]
public static string ComputeContentMd5(byte[] body)
```

Prefer `[SuppressMessage]` with a `Justification` over `#pragma warning disable`: it's scoped to one member, survives refactoring, and states why. Avoid a project-wide `GlobalSuppressions.cs`; it hides findings far from the code. IDE0079 (enforced) reports suppressions that no longer suppress anything, so stale ones don't pile up.

## Day-to-day commands

```powershell
./build.ps1                       # restore, build, test (logs in artifacts/logs)
./build.ps1 -UpdatePublicApi      # accept new public API members
dotnet format                     # apply all auto-fixable style fixes
dotnet format analyzers --diagnostics CA1305 --severity info   # fix one rule across the solution
```

`artifacts/logs/build.binlog` opens in the [MSBuild Structured Log Viewer](https://msbuildlog.com). It shows every property and item, and which file set them, which is the fastest way to answer "why is this rule (not) running?".

## Why not StyleCop.Analyzers?

StyleCop overlaps heavily with the built-in IDE rules (ordering, spacing, braces, documentation) and its 1.2 line has been in beta for years. The built-in rules plus Meziantou cover the same ground with less duplication.

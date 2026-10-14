# SolisCloud API notes

Observations about the SolisCloud Platform API (document v2.0.3) that affect this library. The document itself is not redistributed: it is marked "All rights reserved". Section numbers below refer to it.

Each item is marked **Confirmed** (verified by a test or a real response) or **To verify** (inferred from the document, waiting on real captures).

## Transport and signing

- **Confirmed.** All operations are `POST` with a JSON body, sent to `https://www.soliscloud.com:13333/`. The examples also show `test.soliscloud.com:3333`, and port 13333 can be blocked on restrictive networks, so the base address is configurable.
- **Confirmed.** `Content-MD5` is `Base64(MD5(body bytes))`. The document's example (`{"pageNo":1,"pageSize":10}` gives `kxdxk7rbAsrzSIWgEwhH4w==`) is a unit test.
- **Confirmed.** The signature is `Base64(HMAC-SHA1(secret, "POST\n" + Content-MD5 + "\n" + Content-Type + "\n" + Date + "\n" + path))`. The test value is cross-checked against `openssl`.
- **Content-Type.** Section 2.2 says `application/json;charset=UTF-8`; the worked example in 2.4 uses `application/json`. The library sends and signs `application/json`, because some HTTP stacks re-format media-type parameters, and the server checks the signature against the header it receives. **To verify** against the live API.
- **Date.** The Java sample formats a single-digit day (`d MMM`); the library sends standard RFC 1123 (`dd MMM`) because .NET Framework re-formats the `Date` header that way regardless of what the caller supplies. The server must be within ±15 minutes of the client clock. **To verify** against the live API.
- **Confirmed.** Headers are added with `TryAddWithoutValidation` so .NET sends the exact signed strings. `WireFormatTests` checks the actual bytes on the wire.

## Responses

- Every response is `{ "success", "code", "msg", "data" }`. Success is `code == "0"` and `success` not `false`. Documented error codes are in appendix 1 (`R0000` no authority, `B0049` collector missing or not visible, `I0000` required parameters empty, …).
- Data refreshes every **5 minutes** on the server. Polling faster gains nothing.
- Rate limit: **2 requests/second per operation**, except `stationMonthEnergyList` and `stationYearEnergyList` at 10/second. The response to exceeding it is **To verify**.

## Data quirks (all **To verify** with captures)

- **IDs are 19-digit numbers**, typed as `Number`/`Integer` in the tables but sent as strings in the examples. They exceed the precision of a `double` (and of JavaScript numbers), so they must be handled as strings or `long`, never `double`.
- **Data types are unreliable.** The document went through a find-and-replace that turned "int" and "long" into "Integer": `longitude` became `Integeritude`, `alarmBeginTime` became `alarmBegIntegerime`, and `printStackTrace` became `prIntegerStackTrace`. The real parameter names are `longitude` and `alarmBeginTime`.
- **Values arrive with a separate unit string**: `pac` + `pacStr` (`"kW"`), `pow1` + `pow1Str` (`"W"`), `eTotal` + `eTotalStr` (`"kWh"` or `"MWh"` depending on magnitude). Units must be normalised from the unit field, never assumed.
- **Numbered fields** `uPv1…uPv32`, `iPv1…iPv32`, `pow1…pow32` (DC strings) and `uAc1…uAc3`, `iAc1…iAc3` (AC phases). The real string count is `dcInputType + 1`.
- **Inconsistent names**: `etoday` (list) vs `eToday` (detail), the typo `bstteryCurrent`, and `batteryPowerZheng`/`batteryPowerFu` (pinyin for positive/negative, i.e. power split by direction).
- **Timestamps**: `dataTimestamp` is described as "under UTC+8" but is epoch milliseconds (a string in the examples). Epoch time does not depend on a time zone, so the note is probably wrong.
- **History operations require `money` (currency code) and `timeZone` (integer hours).**
- The example responses contain many fields that the tables don't list. Models must keep unknown fields.

## Write operations

Section 4.11–4.15 operations (add/update station, bind/unbind collector, bind inverter) change account data. The client refuses them unless `AllowWriteOperations` is set.

## Not covered by this document

Inverter control (charge/discharge times, SOC limits, work modes) is a separate Solis API with its own document and access approval. It is out of scope for now.

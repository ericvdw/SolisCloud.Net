# SolisCloud API notes

Observations about the SolisCloud Platform API (document v2.0.3) that affect this library. The document itself is not redistributed: it is marked "All rights reserved". Section numbers below refer to it.

Each item is marked **Confirmed** (verified by a test or a real response) or **To verify** (inferred from the document, waiting on real captures).

The first real capture came from a single-phase hybrid system on 2026-10-14: a Solis S6-EH1P8K-L-PLUS with a battery and a WiFi data logger, in South Africa (UTC+2). All 20 read operations it covers succeeded. Systems with other inverters, three-phase output, EPMs, meters or several inverters are still unseen.

## Transport and signing

- **Confirmed.** All operations are `POST` with a JSON body, sent to `https://www.soliscloud.com:13333/`. The examples also show `test.soliscloud.com:3333`, and port 13333 can be blocked on restrictive networks, so the base address is configurable.
- **Confirmed.** `Content-MD5` is `Base64(MD5(body bytes))`. The document's example (`{"pageNo":1,"pageSize":10}` gives `kxdxk7rbAsrzSIWgEwhH4w==`) is a unit test.
- **Confirmed.** The signature is `Base64(HMAC-SHA1(secret, "POST\n" + Content-MD5 + "\n" + Content-Type + "\n" + Date + "\n" + path))`. The test value is cross-checked against `openssl`.
- **Confirmed.** Section 2.2 says `Content-Type: application/json;charset=UTF-8`; the worked example in 2.4 uses `application/json`. The library sends and signs `application/json`, which the live API accepts. (Some HTTP stacks re-format media-type parameters, and the server checks the signature against the header it receives.)
- **Date.** The Java sample formats a single-digit day (`d MMM`); the library sends standard RFC 1123 (`dd MMM`) because .NET Framework re-formats the `Date` header that way regardless of what the caller supplies. **Confirmed** on a two-digit day; days 1–9, where the two formats differ, are **to verify**. The server must be within ±15 minutes of the client clock.
- **Confirmed.** Headers are added with `TryAddWithoutValidation` so .NET sends the exact signed strings. `WireFormatTests` checks the actual bytes on the wire.

## Responses

- Every response is `{ "success", "code", "msg", "data" }`. Success is `code == "0"` and `success` not `false`. Documented error codes are in appendix 1 (`R0000` no authority, `B0049` collector missing or not visible, `I0000` required parameters empty, …).
- Data refreshes every **5 minutes** on the server. Polling faster gains nothing.
- Rate limit: **2 requests/second per operation**, except `stationMonthEnergyList` and `stationYearEnergyList` at 10/second. The response to exceeding it is **To verify**.

## Data: confirmed from the first capture

### Identifiers and types
- **Confirmed: every ID is a string** (`id`, `stationId`, `inverterId`, `collectorId`, `userId`, `installerId`), even though the tables say `Number`. They are 19 digits, beyond the precision of a `double` and of JavaScript numbers, so models keep them as strings.
- **Confirmed: the same name can mean different things.** `money` is the currency code (`"ZAR"`) in lists and details, but the revenue amount (`58.8`) in history records. `state` is a number almost everywhere but a string (`"2"`) in alarms. Models are per operation, not per field name.
- **Confirmed: data types in the tables are unreliable.** The document went through a find-and-replace that turned "int" and "long" into "Integer": `longitude` became `Integeritude` and `alarmBeginTime` became `alarmBegIntegerime`.
- **Confirmed: undocumented fields are everywhere.** Inverter detail has about 810 fields and station detail about 390, far more than the tables list. Models must keep unknown fields.

### Units
- **Confirmed: in details and lists, the unit string is correct and changes with magnitude.** For example `eToday` is `22.4` with `"kWh"` while `eTotal` is `4.782` with `"MWh"`. Normalise from the unit field.
- **Confirmed: in history data, the unit strings are wrong.** Day series report `pac: 1154` with `pacStr: "kW"` when the value is in watts. Annual records report `money: 952.35` with `moneyStr: "K ZAR"` when the value is in rand. `inverterAll` reports `energy: 4770.6` with `energyStr: "MWh"` when the value is in kWh: lifetime energy in inverter detail is 4.782 MWh. So history values are in base units (W, kWh, currency), and their unit strings must be ignored.
- **Confirmed: `*Zheng` / `*Fu` fields** (pinyin for positive/negative) split a signed value into two non-negative magnitudes, in **watts** and with no unit field. For example `batteryPower: -1.364` (kW) appears as `batteryPowerZheng: 0`, `batteryPowerFu: 1364.0`.

### Sign conventions
- **Confirmed: battery power is positive while charging, negative while discharging.** In the morning `batteryPower` is positive as the state of charge rises; overnight it's negative as it falls.
- **Confirmed: grid power (`psum`) is negative while importing and positive while exporting.** At 05:55, with the battery at its 20% floor, PV produced 300 W, the load used 673 W, and `psum` was `-373`. `psumFu` is the import magnitude. Note this is the opposite of the "positive = import" convention common in home-energy tools such as Home Assistant.
- **Confirmed: `pac` on a hybrid inverter tracks PV generation**, not the AC output to the house.
- **Confirmed: in day series, `familyLoadPower` is usually 0.** The load is in `consumeEnergy` (a power in W, despite the name), which equals `power - psum - batteryPower`.

### Timestamps and time zones
- **Confirmed: `dataTimestamp` in details and day series is a real epoch in milliseconds** (a string). `1791986812838` matches `dataTimestampStr: "2026-10-14 16:06:52 (UTC+02:00)"`. The document's "under UTC+8" note is wrong for these fields.
- **Confirmed: history records are anchored to China time (UTC+8)**, whatever the station's time zone. The station day series' `time` for local midnight is 16:00 UTC, which is midnight in UTC+8. Monthly and annual records carry `timeZone: 8` and a `date` epoch that only makes sense in UTC+8. Use `timeStr` and `dateStr` (local to the station) as the source of truth for history, not the epochs.
- **Confirmed:** the station's own offset is in `timeZone` (`2.0`), and its IANA name is in `timeZoneStandardId`.

### Shapes
- **Confirmed: paged lists put records in `data.page.records`** (`userStationList`, `inverterList`, `collectorList`, `epmList`, `weatherList`, `ammeterList`), **or in `data.records`** (`inverterDetailList`, `alarmList`).
- **Confirmed: history operations return arrays.** Day series have one point per 5 minutes: 194 points by 16:10. Monthly arrays have one record per day so far, annual arrays one per month, and lifetime arrays one per year.
- **Confirmed: numbered fields** `uPv1…uPv32`, `iPv1…iPv32` and `pow1…pow32` are all present, with zeros for unused strings. Inverter day series have 129 fields per point.
- **Confirmed: some string fields hold JSON**, e.g. station detail's `extraInfo` (inverter model, serial numbers, export settings).
- **Confirmed:** history operations require `money` (currency code) and `timeZone` (hours; `2.0` is accepted).

### Still to verify
- What the API returns when the rate limit is exceeded.
- How far back 5-minute day data is kept (needed for backfilling history).
- Three-phase, grid-tie-only, multi-inverter, EPM and meter systems.

## Write operations

Section 4.11–4.15 operations (add/update station, bind/unbind collector, bind inverter) change account data. The client refuses them unless `AllowWriteOperations` is set.

## Not covered by this document

Inverter control (charge/discharge times, SOC limits, work modes) is a separate Solis API with its own document and access approval. It is out of scope for now.

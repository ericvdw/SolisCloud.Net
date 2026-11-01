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
- **Confirmed: some values carry a scale factor, `*Pec`.** The value in the unit of `*Str` is `value × *Pec`. History data uses it:
  - `pac: 1154`, `pacPec: "0.001"`, `pacStr: "kW"` is 1.154 kW.
  - `money: 953.4`, `moneyPec: "0.001"`, `moneyStr: "K ZAR"` is 0.9534 thousand rand, i.e. R953.40.
  - `energy: 4770.9`, `energyPec: "0.001"`, `energyStr: "MWh"` is 4.7709 MWh.

  Read without the factor, these look like wrong units. The library always applies it, and treats a missing factor as 1.
- **Confirmed: a unit can change between records of one series.** In the inverter day series `pacStr` is `"kW"` with `pacPec: "0.001"`, but on the inverter's first day 57 points came as `"W"` with `pacPec: "1"`. Read each record's own unit fields.
- **Confirmed: values without a unit field** in history data (`batteryPower`, `psum`, `consumeEnergy` in day series) are in watts, and energy totals are in kWh.
- **Confirmed: `*Zheng` / `*Fu` fields** (pinyin for positive/negative) split a signed value into two non-negative magnitudes, in **watts** and with no unit field. For example `batteryPower: -1.364` (kW) appears as `batteryPowerZheng: 0`, `batteryPowerFu: 1364.0`.

### Sign conventions
- **Confirmed: battery power is positive while charging, negative while discharging.** In the morning `batteryPower` is positive as the state of charge rises; overnight it's negative as it falls.
- **Confirmed: grid power (`psum`) is negative while importing and positive while exporting.** At 05:55, with the battery at its 20% floor, PV produced 300 W, the load used 673 W, and `psum` was `-373`. `psumFu` is the import magnitude. Note this is the opposite of the "positive = import" convention common in home-energy tools such as Home Assistant.
- **Confirmed: `pac` on a hybrid inverter tracks PV generation**, not the AC output to the house.
- **Confirmed: in the station day series, `familyLoadPower` is usually 0.** The load is in `consumeEnergy` (a power in W, despite the name), which equals `power - psum - batteryPower` exactly at every point.
- **Confirmed: in the inverter day series, the load is split** into the grid-side load (`familyLoadPower`) and the backup-port load (`bypassLoadPower`). Their sum matches `pac - pSum - batteryPower` to within conversion losses. Note the capital S in `pSum` here, where every other response uses `psum`.
  - Ten months of data confirm which is which. `familyLoadPower` is 0 most of the time and about 3.9 kW while a grid-side geyser runs; during a three-day grid outage it stayed at 0 (the geyser had no power) while `bypassLoadPower` carried on.
  - **`familyLoadPower` means something else in details.** Inverter details report it equal to `totalLoadPower`; station details report 0. The library takes the grid-side load in details as `totalLoadPower - bypassLoadPower` instead.
- **Confirmed: `psum` briefly shows export on a zero-export system**, up to about 240 W for one 5-minute point (46 points in ten months), when the load changes sharply. It's the meter catching up, not real export.

### Daily and monthly totals
- **Confirmed: `consumeEnergy` in monthly, annual and lifetime records is calculated, not measured.** It equals `energy + gridPurchasedEnergy - gridSellEnergy - batteryChargeEnergy + batteryDischargeEnergy` exactly, so inverter losses count as consumption: about 1.8 kWh a day (a tenth of generation) on the system checked. `homeLoadEnergy` repeats it. The inverter's own `homeLoadTodayEnergy` in the day series is measured and lower.
- **Confirmed: `backUpEnergy` is the measured backup-port load.** `homeGridEnergy` is what is left, `consumeEnergy - backUpEnergy`: the grid-side load plus the losses.
- **Confirmed: battery charge and discharge totals in monthly records are whole kWh** (`9.0`, `10.0`), while the other totals have decimals. The inverter day series' `batteryTodayChargeEnergy` has a decimal.
- **Confirmed: SolisCloud's own totals can be wrong.** For one system, the annual record for August 2026 and the monthly record for 31 August are all zeros, though the inverter's 5-minute data for those days is normal; re-fetching weeks later gives the same zeros. Where totals matter, check them against the day series.

### Timestamps and time zones
- **Confirmed: `dataTimestamp` in details and day series is a real epoch in milliseconds** (a string). `1791986812838` matches `dataTimestampStr: "2026-10-14 16:06:52 (UTC+02:00)"`. The document's "under UTC+8" note is wrong for these fields.
- **Confirmed: history records are anchored to China time (UTC+8)**, whatever the station's time zone. Even the 5-minute points of the inverter day series carry `timeZone: 8`, although their `dataTimestamp` is a true epoch. The station day series' `time` for local midnight is 16:00 UTC, which is midnight in UTC+8. Monthly and annual records carry `timeZone: 8` and a `date` epoch that only makes sense in UTC+8. Use `timeStr` and `dateStr` (local to the station) as the source of truth for history, not the epochs.
- **Confirmed:** the station's own offset is in `timeZone` (`2.0`), and its IANA name is in `timeZoneStandardId`.

### Shapes
- **Confirmed: paged lists put records in `data.page.records`** (`userStationList`, `inverterList`, `collectorList`, `epmList`, `weatherList`, `ammeterList`), **or in `data.records`** (`inverterDetailList`, `alarmList`).
- **Confirmed: history operations return arrays.** Day series have one point per 5 minutes: 194 points by 16:10. Monthly arrays have one record per day so far, annual arrays one per month, and lifetime arrays one per year.
- **Confirmed: numbered fields** `uPv1…uPv32`, `iPv1…iPv32` and `pow1…pow32` are all present in inverter details, with zeros for unused strings. Inverter day series have 129 fields per point but **no `powN`**: only `uPvN` and `iPvN`, so the client works out each string's DC power as their product (in details, `pow1: 7` for `uPv1: 74.9`, `iPv1: 0.1` shows the inverter does the same, rounded).
- **Confirmed: some string fields hold JSON**, e.g. station detail's `extraInfo` (inverter model, serial numbers, export settings).
- **Confirmed:** history operations require `money` (currency code) and `timeZone` (hours; `2` and `2.0` are accepted).
- **Confirmed: inverter details spell the input count `dcInputtype`** (lower-case t), where the inverter list has `dcInputType`. The number of DC inputs is the value plus one.

### Errors and edge cases (probed against a live account)
SolisCloud rarely reports a bad request as an error. What it does instead:

| Request | Answer |
| --- | --- |
| Wrong API secret | HTTP **403**, not the documented code `R0000` |
| Unknown station ID (`stationDetail`) | success, `data: null`; the client raises `SolisCloudNotFoundException` |
| Unknown inverter serial number (`inverterDetail`) | success, with a placeholder record: a different serial number (for `0000000000000000`, fifteen zeros), no model, `version: "--"`, `dataTimestamp: "0"`. The client raises `SolisCloudNotFoundException` |
| Day series for a future date, a date before installation, or an impossible date (`2026-13-40`) | success, `data: null` (the client returns an empty list) |
| A date not written `yyyy-MM-dd` (`20261030`) | code **`1`**, "Communication error. Please refresh and try again later." |
| `money` or `timeZone` left out of a history request | success; the server fills in defaults |
| `timeZone: 5.5`, or a month written `2026-1` | accepted |
| `pageSize: 1000`, or a page past the end | success |
| 8 requests to one operation at once | all succeed: the documented limit of 2 a second wasn't enforced. The client keeps to it anyway. |

- **Confirmed: 5-minute day data goes back at least to installation** (ten months for the account checked, including its partial first day), so history can be backfilled.

### Data loggers
- **Confirmed: when a data logger loses its connection, SolisCloud raises a "Loss of internet connection" alarm (code `1D4C2`)** for exactly the gap, and its day series (`collector/day`) has no points then.
- **Confirmed: during such a gap the inverter day series may still have points, all with the same values** and grid voltage and frequency near 0. They are placeholders, not readings: don't read them as a grid outage. A "NO-Grid" alarm (`1015`) is the reliable sign of a real outage.
- **Confirmed: the data logger day series' `timeStr` is China time (UTC+8)**, unlike the inverter day series, whose `timeStr` is local. Use `dataTimestamp`.
- **Confirmed: `rssi` is the signal in dBm and `rssiLevel` rates it 1 (weak) to 4 (strong):** about -52 dBm is 4, -82 dBm is 1. On the account checked, signal dropped from about -53 to -82 dBm hours before a disconnection.

### Still to verify
- Three-phase, grid-tie-only, multi-inverter, EPM and meter systems.

## Write operations

Section 4.11–4.15 operations (add/update station, bind/unbind collector, bind inverter) change account data. The client refuses them unless `AllowWriteOperations` is set.

## Not covered by this document

Inverter control (charge/discharge times, SOC limits, work modes) is a separate Solis API with its own document and access approval. It is out of scope for now.

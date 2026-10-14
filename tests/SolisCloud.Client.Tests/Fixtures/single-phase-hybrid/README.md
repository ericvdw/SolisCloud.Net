# Fixture: single-phase hybrid with battery

Captured with `solis capture` on 2026-10-14 at 18:19 (UTC+2), just after sunset, and sanitised:
- IDs and serial numbers are consistent fakes.
- Names, addresses and contact details are `REDACTED`.

| | |
|---|---|
| Inverter | Solis S6-EH1P8K-L-PLUS, 8 kW, two MPPT inputs, single-phase |
| Storage | Battery on the inverter's battery port; load on the backup port |
| Data logger | WiFi stick (S2-WL-ST) |
| Location | South Africa (UTC+2, ZAR) |

Each file is one request and its response: `{ "operation", "request", "response" }`.

To add a fixture from your own system, run `solis capture`, read the files to check nothing personal is left, and open a pull request adding them in a new folder named after the system type.

// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using SolisCloud.Models;

namespace SolisCloud.Internal;

/// <summary>
/// Turns the API's numbered fields (<c>uPv1…uPv32</c>, <c>uAc1…uAc3</c>) into lists.
/// </summary>
internal static class Readings
{
    private const int MaximumPvStrings = 32;

    /// <summary>
    /// Returns the inverter's DC inputs. The count comes from <c>dcInputType</c> (number of inputs minus one; the
    /// details spell it <c>dcInputtype</c>). Without it, inputs are kept up to the last one reporting anything.
    /// </summary>
    public static IReadOnlyList<PvString> PvStrings(FieldReader f)
    {
        int? inputType = f.Int32("dcInputType") ?? f.Int32("dcInputtype");
        int count = inputType is >= 0 ? Math.Min(inputType.Value + 1, MaximumPvStrings) : LastActivePvString(f);

        List<PvString> strings = new(count);
        for (int i = 1; i <= count; i++)
        {
            string n = i.ToString(CultureInfo.InvariantCulture);
            strings.Add(new PvString(i, f.Number("uPv" + n), f.Number("iPv" + n), f.Power("pow" + n, "W")));
        }

        return strings;
    }

    /// <summary>
    /// Returns the AC output phases: one for a single-phase inverter (<c>acOutputType</c> 0), otherwise three.
    /// Empty if the record carries no AC readings.
    /// </summary>
    public static IReadOnlyList<AcPhase> AcPhases(FieldReader f)
    {
        if (f.Number("uAc1") is null && f.Number("iAc1") is null)
        {
            return [];
        }

        int count = f.Int32("acOutputType") == 0 ? 1 : 3;
        List<AcPhase> phases = new(count);
        for (int i = 1; i <= count; i++)
        {
            string n = i.ToString(CultureInfo.InvariantCulture);
            phases.Add(new AcPhase(i, f.Number("uAc" + n), f.Number("iAc" + n)));
        }

        return phases;
    }

    private static int LastActivePvString(FieldReader f)
    {
        for (int i = MaximumPvStrings; i >= 1; i--)
        {
            string n = i.ToString(CultureInfo.InvariantCulture);
            if (f.Number("uPv" + n) is > 0 || f.Number("iPv" + n) is > 0 || f.Number("pow" + n) is > 0)
            {
                return i;
            }
        }

        return 0;
    }
}

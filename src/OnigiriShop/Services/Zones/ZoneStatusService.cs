using System;
using System.Collections.Generic;
using System.Linq;
using OnigiriShop.Data.Models.Zones;

namespace OnigiriShop.Services.Zones;

public interface IZoneStatusService
{
    IReadOnlyList<ZoneCountingSummary> BuildSummaries(IEnumerable<CountingRecord> records);
}

public sealed class ZoneStatusService : IZoneStatusService
{
    private static readonly IReadOnlyList<string> ZoneCodes = BuildZoneCodes();
    private static readonly HashSet<string> ZoneCodeSet = new(ZoneCodes, StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyDictionary<string, string> _labels;

    public ZoneStatusService()
        : this(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
    {
    }

    public ZoneStatusService(IReadOnlyDictionary<string, string>? labels)
    {
        _labels = labels is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(labels, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ZoneCountingSummary> BuildSummaries(IEnumerable<CountingRecord> records)
    {
        var countingLookup = new Dictionary<string, List<CountingRecord>>(StringComparer.OrdinalIgnoreCase);
        if (records != null)
        {
            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.ZoneCode))
                {
                    continue;
                }

                var normalizedCode = Normalize(record.ZoneCode);
                if (!ZoneCodeSet.Contains(normalizedCode))
                {
                    continue;
                }

                if (!countingLookup.TryGetValue(normalizedCode, out var list))
                {
                    list = new List<CountingRecord>();
                    countingLookup[normalizedCode] = list;
                }

                list.Add(record);
            }
        }

        var summaries = new List<ZoneCountingSummary>(ZoneCodes.Count);
        foreach (var code in ZoneCodes)
        {
            countingLookup.TryGetValue(code, out var zoneRecords);

            var inProgress = zoneRecords?
                .Where(r => r.State == CountingState.InProgress)
                .OrderByDescending(r => r.StartedAt)
                .Select(ToSnapshot)
                .FirstOrDefault();

            var completed = zoneRecords?
                .Where(r => r.State == CountingState.Completed)
                .OrderByDescending(r => r.CompletedAt ?? r.StartedAt)
                .Select(ToSnapshot)
                .FirstOrDefault();

            summaries.Add(new ZoneCountingSummary(code, TryGetLabel(code), inProgress, completed));
        }

        return summaries;
    }

    private static CountingSnapshot ToSnapshot(CountingRecord record) =>
        new(record.StartedAt, record.CompletedAt, record.State, record.Reference);

    private string? TryGetLabel(string code) =>
        _labels.TryGetValue(code, out var label) && !string.IsNullOrWhiteSpace(label)
            ? label
            : null;

    private static string Normalize(string code) => code.Trim().ToUpperInvariant();

    private static IReadOnlyList<string> BuildZoneCodes()
    {
        var codes = new List<string>(39);
        for (var i = 1; i <= 20; i++)
        {
            codes.Add($"B{i}");
        }

        for (var i = 1; i <= 19; i++)
        {
            codes.Add($"S{i}");
        }

        return codes.AsReadOnly();
    }
}

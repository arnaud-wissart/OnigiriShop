namespace OnigiriShop.Data.Models.Zones;

public sealed record CountingRecord(
    string ZoneCode,
    CountingState State,
    DateTime StartedAt,
    DateTime? CompletedAt = null,
    string? Reference = null);

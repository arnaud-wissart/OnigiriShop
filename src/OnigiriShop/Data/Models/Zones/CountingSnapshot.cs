namespace OnigiriShop.Data.Models.Zones;

public sealed record CountingSnapshot(
    DateTime StartedAt,
    DateTime? CompletedAt,
    CountingState State,
    string? Reference);

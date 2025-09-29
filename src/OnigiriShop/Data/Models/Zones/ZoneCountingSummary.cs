namespace OnigiriShop.Data.Models.Zones;

public sealed record ZoneCountingSummary(
    string Code,
    string? Label,
    CountingSnapshot? InProgress,
    CountingSnapshot? LastCompleted);

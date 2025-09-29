using System;
using System.Collections.Generic;
using System.Linq;
using OnigiriShop.Data.Models.Zones;
using OnigiriShop.Services.Zones;

namespace Tests.Unit;

public class ZoneStatusServiceTests
{
    [Fact]
    public void BuildSummaries_ShouldReturnAllThirtyNineZones()
    {
        var service = new ZoneStatusService();

        var result = service.BuildSummaries(Array.Empty<CountingRecord>());

        Assert.Equal(39, result.Count);
        Assert.Equal("B1", result[0].Code);
        Assert.Equal("B2", result[1].Code);
        Assert.Equal("B3", result[2].Code);
        Assert.Equal("B20", result[19].Code);
        Assert.Equal("S1", result[20].Code);
        Assert.Equal("S19", result[^1].Code);
        Assert.All(result, r =>
        {
            Assert.Null(r.InProgress);
            Assert.Null(r.LastCompleted);
        });
    }

    [Fact]
    public void BuildSummaries_ShouldExposeLatestCountingStates()
    {
        var now = DateTime.UtcNow;
        var records = new List<CountingRecord>
        {
            new("B5", CountingState.Completed, now.AddDays(-4), now.AddDays(-4).AddHours(2), "Inventaire #1"),
            new("B5", CountingState.InProgress, now.AddHours(-6), Reference: "Inventaire #2"),
            new("B5", CountingState.InProgress, now.AddHours(-2), Reference: "Inventaire #3"),
            new("B7", CountingState.Completed, now.AddDays(-7), now.AddDays(-7).AddHours(1)),
            new("B7", CountingState.Completed, now.AddDays(-1), now.AddDays(-1).AddHours(3), "Inventaire #4"),
            new("S3", CountingState.InProgress, now.AddHours(-1), Reference: "Inventaire #5"),
            new("S3", CountingState.Completed, now.AddDays(-2), now.AddDays(-2).AddHours(2)),
            new("X1", CountingState.InProgress, now) // zone inconnue ignorée
        };

        var service = new ZoneStatusService();

        var result = service.BuildSummaries(records);

        var zoneB5 = result.Single(r => r.Code == "B5");
        Assert.NotNull(zoneB5.InProgress);
        Assert.True(Math.Abs((zoneB5.InProgress!.StartedAt - now.AddHours(-2)).TotalSeconds) <= 1);
        Assert.Equal("Inventaire #3", zoneB5.InProgress.Reference);
        Assert.NotNull(zoneB5.LastCompleted);
        var b5CompletedAt = zoneB5.LastCompleted!.CompletedAt;
        Assert.NotNull(b5CompletedAt);
        Assert.True(Math.Abs((b5CompletedAt!.Value - now.AddDays(-4).AddHours(2)).TotalSeconds) <= 1);

        var zoneB7 = result.Single(r => r.Code == "B7");
        Assert.Null(zoneB7.InProgress);
        Assert.NotNull(zoneB7.LastCompleted);
        Assert.Equal("Inventaire #4", zoneB7.LastCompleted!.Reference);
        var b7CompletedAt = zoneB7.LastCompleted!.CompletedAt;
        Assert.NotNull(b7CompletedAt);
        Assert.True(Math.Abs((b7CompletedAt!.Value - now.AddDays(-1).AddHours(3)).TotalSeconds) <= 1);

        var zoneS3 = result.Single(r => r.Code == "S3");
        Assert.NotNull(zoneS3.InProgress);
        Assert.NotNull(zoneS3.LastCompleted);
        Assert.Equal("Inventaire #5", zoneS3.InProgress!.Reference);

        var zoneS4 = result.Single(r => r.Code == "S4");
        Assert.Null(zoneS4.InProgress);
        Assert.Null(zoneS4.LastCompleted);
    }

    [Fact]
    public void BuildSummaries_ShouldRespectProvidedLabels()
    {
        var labels = new Dictionary<string, string>
        {
            ["B1"] = "Zone existante",
            ["S2"] = "Allée Sud 2",
            ["S3"] = string.Empty
        };

        var service = new ZoneStatusService(labels);

        var result = service.BuildSummaries(new[]
        {
            new CountingRecord("B1", CountingState.InProgress, DateTime.UtcNow)
        });

        Assert.Equal("Zone existante", result.Single(r => r.Code == "B1").Label);
        Assert.Equal("Allée Sud 2", result.Single(r => r.Code == "S2").Label);
        Assert.Null(result.Single(r => r.Code == "S3").Label);
    }
}

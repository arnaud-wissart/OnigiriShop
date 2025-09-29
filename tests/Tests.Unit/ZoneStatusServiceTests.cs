using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
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

        result.Should().HaveCount(39);
        result.Select(r => r.Code).Should().StartWith(["B1", "B2", "B3"]);
        result[19].Code.Should().Be("B20");
        result[20].Code.Should().Be("S1");
        result.Last().Code.Should().Be("S19");
        result.Should().OnlyContain(r => r.InProgress is null && r.LastCompleted is null);
    }

    [Fact]
    public void BuildSummaries_ShouldExposeLatestCountingStates()
    {
        var now = DateTime.UtcNow;
        var records = new List<CountingRecord>
        {
            new("B5", CountingState.Completed, now.AddDays(-4), now.AddDays(-4).AddHours(2), "Inventaire #1"),
            new("B5", CountingState.InProgress, now.AddHours(-6), reference: "Inventaire #2"),
            new("B5", CountingState.InProgress, now.AddHours(-2), reference: "Inventaire #3"),
            new("B7", CountingState.Completed, now.AddDays(-7), now.AddDays(-7).AddHours(1)),
            new("B7", CountingState.Completed, now.AddDays(-1), now.AddDays(-1).AddHours(3), "Inventaire #4"),
            new("S3", CountingState.InProgress, now.AddHours(-1), reference: "Inventaire #5"),
            new("S3", CountingState.Completed, now.AddDays(-2), now.AddDays(-2).AddHours(2)),
            new("X1", CountingState.InProgress, now) // zone inconnue ignorée
        };

        var service = new ZoneStatusService();

        var result = service.BuildSummaries(records);

        var zoneB5 = result.Single(r => r.Code == "B5");
        zoneB5.InProgress.Should().NotBeNull();
        zoneB5.InProgress!.StartedAt.Should().BeCloseTo(now.AddHours(-2), TimeSpan.FromSeconds(1));
        zoneB5.InProgress.Reference.Should().Be("Inventaire #3");
        zoneB5.LastCompleted.Should().NotBeNull();
        zoneB5.LastCompleted!.CompletedAt.Should().BeCloseTo(now.AddDays(-4).AddHours(2), TimeSpan.FromSeconds(1));

        var zoneB7 = result.Single(r => r.Code == "B7");
        zoneB7.InProgress.Should().BeNull();
        zoneB7.LastCompleted.Should().NotBeNull();
        zoneB7.LastCompleted!.Reference.Should().Be("Inventaire #4");
        zoneB7.LastCompleted.CompletedAt.Should().BeCloseTo(now.AddDays(-1).AddHours(3), TimeSpan.FromSeconds(1));

        var zoneS3 = result.Single(r => r.Code == "S3");
        zoneS3.InProgress.Should().NotBeNull();
        zoneS3.LastCompleted.Should().NotBeNull();
        zoneS3.InProgress!.Reference.Should().Be("Inventaire #5");

        var zoneS4 = result.Single(r => r.Code == "S4");
        zoneS4.InProgress.Should().BeNull();
        zoneS4.LastCompleted.Should().BeNull();
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

        result.Single(r => r.Code == "B1").Label.Should().Be("Zone existante");
        result.Single(r => r.Code == "S2").Label.Should().Be("Allée Sud 2");
        result.Single(r => r.Code == "S3").Label.Should().BeNull();
    }
}

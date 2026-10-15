using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Ids;

namespace Sovereigns.Simulation.Tests;

/// <summary>SOV-P03-T06: continuous features with durable identities.</summary>
public sealed class FeatureTests
{
    private static World NewWorld() => World.Create(TestSupport.Scenario(east: 100_000, north: 100_000), seed: 1);

    [Fact]
    public void DrawnLinesGetDurableIdsAndAreFoundByDistance()
    {
        var world = NewWorld();
        world.Submit(new DrawSurveyLine([new WorldPoint(1_000, 1_000), new WorldPoint(3_000, 1_000)], "ford road"));
        world.Submit(new DrawSurveyLine([new WorldPoint(1_000, 1_100), new WorldPoint(3_000, 1_100)], "bank"));

        var hits = world.Inspect(new WorldPoint(2_000, 1_030), markerRadiusM: 100).NearbyFeatures;

        Assert.Equal(["survey:1", "survey:2"], hits.Select(hit => hit.Feature.Id));
        Assert.Equal(30, hits[0].DistanceM, 9);
        Assert.Equal(new WorldPoint(2_000, 1_000), hits[0].Nearest);
        Assert.Equal(WorldEventType.FeatureDrawn, world.Ledger.Events[^1].Type);
    }

    [Fact]
    public void ALongDiagonalLineIsIndexedAlongItsLengthAndFoundEverywhere()
    {
        var world = NewWorld();
        world.Submit(new DrawSurveyLine([new WorldPoint(10, 10), new WorldPoint(99_990, 99_990)], "diagonal"));

        foreach (var t in new[] { 0.01, 0.37, 0.5, 0.93 })
        {
            var on = new WorldPoint(10 + 99_980 * t, 10 + 99_980 * t);
            var hit = Assert.Single(world.Inspect(new WorldPoint(on.X + 5, on.Y - 5), markerRadiusM: 10).NearbyFeatures);
            Assert.Equal(Math.Sqrt(50), hit.DistanceM, 6);
        }

        Assert.Empty(world.Inspect(new WorldPoint(50_000, 10_000), markerRadiusM: 1_000).NearbyFeatures);
        Assert.Equal(1, world.FeatureIndexComputations);
    }

    [Fact]
    public void ChangingALineRefreshesTheIndexItsQueriesUse()
    {
        // SOV-P03-T11: a drawn or removed line is seen by the next query; unchanged lines reuse the index.
        var world = NewWorld();
        var near = new WorldPoint(5_000, 5_010);
        world.Submit(new DrawSurveyLine([new WorldPoint(4_000, 5_000), new WorldPoint(6_000, 5_000)], "road"));
        Assert.Single(world.Inspect(near, markerRadiusM: 20).NearbyFeatures);
        Assert.Single(world.Inspect(near, markerRadiusM: 20).NearbyFeatures);
        Assert.Equal(1, world.FeatureIndexComputations);

        world.Submit(new RemoveSurveyLine(new EntityId<Kind.SurveyLine>(1)));

        Assert.Empty(world.Inspect(near, markerRadiusM: 20).NearbyFeatures);
        Assert.Equal(2, world.FeatureIndexComputations);
    }

    [Fact]
    public void RefinementKeepsEveryOriginalVertexInOrder()
    {
        WorldPoint[] coarse = [new(0, 0), new(1_000, 0), new(1_000, 750)];

        var fine = FeatureGeometry.Densify(coarse, 100);

        Assert.Equal(coarse, fine.Where(coarse.Contains));
        Assert.Equal(19, fine.Count);
        Assert.All(fine.Zip(fine.Skip(1)), pair => Assert.InRange(
            Math.Sqrt(Math.Pow(pair.Second.X - pair.First.X, 2) + Math.Pow(pair.Second.Y - pair.First.Y, 2)), 1, 100 + 1e-9));
    }

    [Fact]
    public void RemovedLinesLeaveAndTheirIdsAreNeverReused()
    {
        var world = NewWorld();
        world.Submit(new DrawSurveyLine([new WorldPoint(1, 1), new WorldPoint(2, 2)], "a"));
        world.Submit(new RemoveSurveyLine(new EntityId<Kind.SurveyLine>(1)));
        world.Submit(new DrawSurveyLine([new WorldPoint(3, 3), new WorldPoint(4, 4)], "b"));

        Assert.Equal(["survey:2"], world.Features.Select(feature => feature.Id));
        Assert.Contains("already removed", world.Submit(new RemoveSurveyLine(new EntityId<Kind.SurveyLine>(1))).Rejection, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidLinesAreRejected()
    {
        var world = NewWorld();
        WorldPoint[][] invalid =
        [
            [new(1, 1)],
            [new(1, 1), new(1, 1)],
            [new(1, 1), new(-5, 1)],
            [.. Enumerable.Range(0, DrawSurveyLine.MaxPoints + 1).Select(k => new WorldPoint(k + 1, 1))],
        ];

        Assert.All(invalid, points => Assert.NotNull(world.Submit(new DrawSurveyLine(points, "bad")).Rejection));
        Assert.Empty(world.Features);
    }
}

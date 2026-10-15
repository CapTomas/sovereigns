using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Random;

namespace Sovereigns.Simulation.Tests;

/// <summary>SOV-P03: the multi-resolution field atlas (ADR-0009).</summary>
public sealed class AtlasTests
{
    private const string Scratch = "scratch";
    private const string Gradient = "scratch_gradient";

    private static readonly FieldDefinition Water = FieldCatalog.Scratch with { Key = "test_water", Refinement = Refinement.Conserve };

    private static World NewWorld() => World.Create(TestSupport.Scenario(east: 40_000, north: 30_000), seed: 1);

    private static World WorldWithWater()
    {
        var world = NewWorld();
        world.Atlas.Register(new GridProvider(Water, new ConstantBase([0.0])));
        return world;
    }

    private static void Paint(World world, double x, double y, double radius, double value) =>
        Assert.Null(world.Submit(new PaintScratch(new WorldPoint(x, y), radius, value)).Rejection);

    [Fact]
    public void LevelsNestExactlyAndChunksTileTheWorld()
    {
        Assert.Equal([4096.0, 256, 16, 1], Enum.GetValues<GridLevel>().Select(Grid.CellSize));
        Assert.Equal(16, Grid.Factor(GridLevel.Regional, GridLevel.Local));
        Assert.Equal(4096, Grid.Factor(GridLevel.Macro, GridLevel.Tactical));
        var cell = Grid.CellAt(GridLevel.Local, new WorldPoint(1_000, 500));
        Assert.Equal(new GridCell(GridLevel.Local, 62, 31), cell);
        Assert.Equal(new GridCell(GridLevel.Regional, 3, 1), Grid.Parent(cell, GridLevel.Regional));
        Assert.Equal(new ChunkKey(GridLevel.Local, -1, -1), Grid.ChunkAt(GridLevel.Local, new WorldPoint(-0.5, -0.5)));
        Assert.Equal((1024.0, 0.0, 2048.0, 1024.0), new ChunkKey(GridLevel.Local, 1, 0).Bounds);
    }

    [Fact]
    public void CatalogGivesEveryFamilyAContractAndListsWhatIsImplemented()
    {
        var keys = FieldCatalog.All.Select(field => field.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.All(FieldFamilies.All, family => Assert.NotEmpty(FieldCatalog.InFamily(family)));
        Assert.All(FieldCatalog.All, field =>
        {
            Assert.False(string.IsNullOrWhiteSpace(field.Unit));
            Assert.True(field.Min < field.Max, field.Key);
            Assert.All(field.DependsOn, dependency => Assert.Contains(dependency, keys));
            Assert.Matches("^Phase \\d\\d$", field.ImplementedBy);
        });
        var world = NewWorld();
        Assert.Equal(["latitude", "local_solar_time", Scratch, Gradient], keys.Where(world.Atlas.IsAvailable));
    }

    [Fact]
    public void SamplingInterpolatesCellCentresAndReportsItsInputs()
    {
        var world = NewWorld();
        var cell = new GridCell(GridLevel.Local, 100, 100);
        Paint(world, cell.Centre.X, cell.Centre.Y, 1, 0.8);

        var atCentre = world.Atlas.Sample(Scratch, cell.Centre);
        var halfway = world.Atlas.Sample(Scratch, new WorldPoint(cell.Centre.X + 8, cell.Centre.Y));

        Assert.Equal(0.8, atCentre.Value);
        Assert.Equal(0.4, halfway.Value!.Value, 12);
        Assert.Equal(4, halfway.Inputs.Count);
        Assert.Equal(1.0, halfway.Inputs.Sum(input => input.Weight), 12);
        Assert.Contains(halfway.Inputs, input => input.Stored && input.Cell == cell && input.Values[0] == 0.8);
        Assert.Equal(ReadingBasis.Stored, halfway.Basis);
        Assert.Equal(world.Now, halfway.ChangedAt);
    }

    [Fact]
    public void UnavailableFieldsAndOutsidePointsNeverInventValues()
    {
        var world = NewWorld();

        var elevation = world.Atlas.Sample("elevation", new WorldPoint(100, 100));
        var outside = world.Atlas.Sample(Scratch, new WorldPoint(-1, 100));

        Assert.Null(elevation.Values);
        Assert.Contains("World foundation implements elevation above datum in Phase 04", elevation.UnavailableReason, StringComparison.Ordinal);
        Assert.Null(outside.Values);
        Assert.All(world.Atlas.SampleArea("elevation", GridLevel.Regional, 0, 0, 256, 4, 4), value => Assert.True(double.IsNaN(value)));
    }

    [Fact]
    public void FieldsAreContinuousAcrossChunkSeamsAndWorldEdges()
    {
        // SOV-P03-T12: a ramp painted across a local chunk seam samples smoothly through the seam.
        var world = NewWorld();
        var seam = Grid.ChunkSize(GridLevel.Local);
        var row = new GridCell(GridLevel.Local, 0, 31).Centre.Y;
        for (var k = -8; k <= 8; k++)
        {
            Paint(world, seam + (k + 0.5) * 16, row, 1, (k + 8) / 20.0);
        }

        double At(double x) => world.Atlas.Sample(Scratch, new WorldPoint(x, row)).Value!.Value;
        Assert.Equal(16, world.Atlas.OverrideCount(Scratch));
        Assert.Equal(0.05 / 16 * 112, At(seam + 56) - At(seam - 56), 12);
        for (var x = seam - 64.0; x <= seam + 64; x += 0.25)
        {
            Assert.Equal(0.05 / 16 * 0.25, At(x + 0.25) - At(x), 12);
        }

        Assert.Equal(At(seam - 1e-9), At(seam), 9);
        Assert.NotNull(world.Atlas.Sample(Scratch, new WorldPoint(0.001, 0.001)).Values);
        Assert.NotNull(world.Atlas.Sample(Scratch, new WorldPoint(39_999.999, 29_999.999)).Values);
    }

    [Fact]
    public void CoarserLevelsSeeExactMeansOfNativeCells()
    {
        // SOV-P03-T09: a regional cell's value is the mean of its 16 × 16 local cells.
        var world = NewWorld();
        var regional = new GridCell(GridLevel.Regional, 10, 7);
        Paint(world, regional.Centre.X, regional.Centre.Y, 60, 1.0);
        var painted = world.Atlas.Sample(Scratch, regional.Centre, GridLevel.Local);
        var nativeSum = 0.0;
        for (var j = 0; j < 16; j++)
        {
            for (var i = 0; i < 16; i++)
            {
                nativeSum += world.Atlas.Sample(Scratch, new GridCell(GridLevel.Local, regional.I * 16 + i, regional.J * 16 + j).Centre).Value!.Value;
            }
        }

        var coarse = world.Atlas.Sample(Scratch, regional.Centre, GridLevel.Regional);

        Assert.Equal(1.0, painted.Value);
        Assert.Equal(nativeSum / 256, coarse.Value!.Value, 12);
        Assert.Equal(ReadingBasis.Derived, coarse.Basis);
        Assert.StartsWith("aggregate", coarse.Classification, StringComparison.Ordinal);
        Assert.Equal(world.Now, coarse.ChangedAt);
    }

    [Fact]
    public void AreaStatisticsWeightCellsByOverlapSoTotalsAreExact()
    {
        var world = NewWorld();
        Paint(world, 2_000, 2_000, 200, 0.5);
        var cellArea = 16.0 * 16.0;
        var painted = world.Atlas.OverrideCount(Scratch);

        var whole = world.Atlas.Aggregate(Scratch, 0, 0, 40_000, 30_000);
        var half = world.Atlas.Aggregate(Scratch, 2_000, 0, 2_008, 30_000);

        Assert.Equal(0.5 * painted * cellArea, whole.Mean * whole.AreaM2, 6);
        Assert.Equal((0.0, 0.5), (whole.Min, whole.Max));
        Assert.Equal(8 * 30_000.0, half.AreaM2);
        Assert.True(half.Mean > 0);
    }

    private static double MeanOfTactical(World world, string field, GridCell cell)
    {
        var sum = 0.0;
        for (var j = 0; j < 16; j++)
        {
            for (var i = 0; i < 16; i++)
            {
                sum += world.Atlas.Sample(field, new GridCell(GridLevel.Tactical, cell.I * 16 + i, cell.J * 16 + j).Centre, GridLevel.Tactical).Value!.Value;
            }
        }

        return sum / 256;
    }

    [Fact]
    public void ConservativeRefinementPreservesEachNativeCellsMean()
    {
        // A curved profile: interpolation alone changes cell means, so conservation has work to do.
        var world = WorldWithWater();
        for (var j = 60; j <= 66; j++)
        {
            for (var i = 58; i <= 70; i++)
            {
                var centre = new GridCell(GridLevel.Local, i, j).Centre;
                var value = Math.Pow((i - 57) / 14.0, 2);
                world.Atlas.Paint(Water.Key, centre, 1, value);
                Paint(world, centre.X, centre.Y, 1, value);
            }
        }

        foreach (var cell in new[] { new GridCell(GridLevel.Local, 64, 63), new GridCell(GridLevel.Local, 59, 61) })
        {
            var native = world.Atlas.Sample(Water.Key, cell.Centre).Value!.Value;
            Assert.Equal(native, MeanOfTactical(world, Water.Key, cell), 12);
            Assert.NotEqual(native, MeanOfTactical(world, Scratch, cell), 6);
        }

        Assert.Equal(Residency.Refined, world.Atlas.ResidencyAt(Water.Key, GridLevel.Tactical, new GridCell(GridLevel.Local, 64, 63).Centre));
        Assert.Equal(Residency.Loaded, world.Atlas.ResidencyAt(Scratch, GridLevel.Tactical, new GridCell(GridLevel.Local, 64, 63).Centre));
        world.Atlas.Unload();
        Assert.Equal(Residency.Unloaded, world.Atlas.ResidencyAt(Water.Key, GridLevel.Tactical, new GridCell(GridLevel.Local, 64, 63).Centre));
    }

    [Fact]
    public void ConservativeRefinementStaysWithinTheFieldsRange()
    {
        // An empty cell among full ones would dip below 0 at its centre, and a full cell among lower ones would rise
        // above 1: the limiter keeps both inside the range and both means exact.
        var world = WorldWithWater();
        var hole = new GridCell(GridLevel.Local, 64, 63);
        var peak = new GridCell(GridLevel.Local, 164, 63);
        world.Atlas.Paint(Water.Key, hole.Centre, 40, 0.8);
        world.Atlas.Paint(Water.Key, hole.Centre, 1, 0.0);
        world.Atlas.Paint(Water.Key, peak.Centre, 40, 0.2);
        world.Atlas.Paint(Water.Key, peak.Centre, 1, 1.0);

        foreach (var centre in new[] { hole, peak })
        {
            for (var b = -2; b <= 2; b++)
            {
                for (var a = -2; a <= 2; a++)
                {
                    var cell = new GridCell(GridLevel.Local, centre.I + a, centre.J + b);
                    Assert.Equal(world.Atlas.Sample(Water.Key, cell.Centre).Value!.Value, MeanOfTactical(world, Water.Key, cell), 12);
                    for (var k = 0; k < 16; k++)
                    {
                        var point = new WorldPoint(cell.Centre.X - 7.5 + k, cell.Centre.Y + 7.5 - k);
                        Assert.InRange(world.Atlas.Sample(Water.Key, point, GridLevel.Tactical).Value!.Value, 0, 1);
                    }
                }
            }
        }

        Assert.Equal(0, world.Atlas.Sample(Water.Key, hole.Centre, GridLevel.Tactical).Value);
        Assert.Equal(1, world.Atlas.Sample(Water.Key, peak.Centre, GridLevel.Tactical).Value);
    }

    [Fact]
    public void GradientIsAVectorFieldDerivedFromItsSource()
    {
        // SOV-P03-T05: a ramp rising 1/16 per local cell eastward has gradient (1/256 m-1, 0).
        var world = NewWorld();
        for (var k = 0; k < 16; k++)
        {
            Paint(world, (190 + k + 0.5) * 16, 190.5 * 16, 1, (k + 1) / 16.0);
        }

        var sample = world.Atlas.Sample(Gradient, new GridCell(GridLevel.Local, 198, 190).Centre);

        Assert.Equal(2, sample.Values!.Count);
        Assert.Equal(1.0 / 256, sample.Values[0], 12);
        Assert.Equal(sample.Values[0], sample.Value!.Value, 6);
        Assert.Equal(ReadingBasis.Derived, sample.Basis);
        Assert.Equal("m-1", sample.Field.Unit);
    }

    [Fact]
    public void GradientSeesStepsBetweenUniformBaseChunks()
    {
        // Each chunk of this base is uniform, but neighbouring chunks differ: the seam has a gradient.
        var world = NewWorld();
        var step = FieldCatalog.Scratch with { Key = "test_step", Class = FieldClass.Base };
        world.Atlas.Register(new GridProvider(step, new StepBase()));
        world.Atlas.Register(new GradientProvider(FieldCatalog.ScratchGradient with { Key = "test_step_gradient", DependsOn = ["test_step"] }, "test_step"));

        var seamCell = world.Atlas.Sample("test_step_gradient", new GridCell(GridLevel.Local, 63, 10).Centre);
        var inside = world.Atlas.Sample("test_step_gradient", new GridCell(GridLevel.Local, 30, 10).Centre);

        Assert.Equal(0.5 / 32, seamCell.Values![0], 15);
        Assert.Equal([0.0, 0.0], inside.Values);
        Assert.Equal(0.5 / 32 / 16, world.Atlas.Sample("test_step_gradient", new GridCell(GridLevel.Regional, 3, 0).Centre, GridLevel.Regional).Values![0], 15);
    }

    [Fact]
    public void CategoryFieldsAreRefusedUntilMajorityAggregationExists()
    {
        var world = NewWorld();

        Assert.Throws<NotSupportedException>(() => world.Atlas.Register(new GridProvider(FieldCatalog.Get("lithology"), new ConstantBase([0.0]))));
    }

    [Fact]
    public void ChangeTimesCoverEveryCellASampleDependsOn()
    {
        // SOV-P03-T13: "last update" includes erasures and the neighbours a derived value reads.
        var world = NewWorld();
        var cell = new GridCell(GridLevel.Local, 300, 300);
        var east = new GridCell(GridLevel.Local, 301, 300);
        world.AdvanceTo(world.Now.Plus(60_000));
        Paint(world, cell.Centre.X, cell.Centre.Y, 1, 0.6);
        var paintedAt = world.Now;

        Assert.NotEqual(0, world.Atlas.Sample(Gradient, east.Centre).Values![0]);
        Assert.Equal(paintedAt, world.Atlas.Sample(Gradient, east.Centre).ChangedAt);

        world.AdvanceTo(world.Now.Plus(60_000));
        Paint(world, cell.Centre.X, cell.Centre.Y, 1, 0);
        var erasedAt = world.Now;

        Assert.Equal(0, world.Atlas.Sample(Scratch, cell.Centre).Value);
        Assert.Equal(erasedAt, world.Atlas.Sample(Scratch, cell.Centre).ChangedAt);
        Assert.Equal(erasedAt, world.Atlas.Sample(Gradient, east.Centre).ChangedAt);
        Assert.Equal(erasedAt, world.Atlas.Sample(Scratch, cell.Centre, GridLevel.Macro).ChangedAt);
        Assert.Equal(world.Identity.Start, world.Atlas.Sample(Scratch, new WorldPoint(30_000, 20_000)).ChangedAt);
    }

    [Fact]
    public void CoarseMeansWeightEdgeCellsByTheirPartInsideTheWorld()
    {
        // 40 km is 156.25 regional cells: the last regional column is a quarter inside the world.
        var world = NewWorld();
        var regional = FieldCatalog.Scratch with { Key = "test_regional", NativeLevel = GridLevel.Regional };
        world.Atlas.Register(new GridProvider(regional, new ConstantBase([0.0])));
        world.Atlas.Paint("test_regional", new WorldPoint(39_950, 2_000), 600, 1.0);
        var macro = new GridCell(GridLevel.Macro, 9, 0);

        var coarse = world.Atlas.Sample("test_regional", macro.Centre, GridLevel.Macro).Value!.Value;
        var aggregate = world.Atlas.Aggregate("test_regional", 9 * 4096, 0, 10 * 4096, 4096);

        Assert.True(coarse > 0);
        Assert.Equal(aggregate.Mean, coarse, 12);
    }

    [Fact]
    public void ChangesInvalidateOnlyDependentChunksNearThem()
    {
        // SOV-P03-T11: painting refreshes the gradient nearby; a distant cached gradient chunk is reused.
        var world = NewWorld();
        Paint(world, 1_000, 1_000, 50, 1.0);
        var near = new WorldPoint(1_000 + 40, 1_000);
        var far = new WorldPoint(30_000, 20_000);
        var before = world.Atlas.Sample(Gradient, near).Value!.Value;
        world.Atlas.Sample(Gradient, far);
        var computations = world.Atlas.Computations(Gradient, ChunkContent.Derived);

        Paint(world, 1_000, 1_000, 50, 0.25);
        world.Atlas.Sample(Gradient, far);
        var after = world.Atlas.Sample(Gradient, near).Value!.Value;

        Assert.True(after < before);
        Assert.Equal(computations + 1, world.Atlas.Computations(Gradient, ChunkContent.Derived));
        Assert.True(world.Atlas.CacheStatistics.Invalidations > 0);
    }

    [Fact]
    public void WritesRefreshEveryCachedLevelAndKind()
    {
        // SOV-P03-T10, T11: paints interleaved with queries; a warm cache must answer exactly like no cache at all.
        IReadOnlyList<string> Run(bool unloadBeforeEachQuery)
        {
            var world = WorldWithWater();
            var strokes = RandomStream.For(3, "test.strokes");
            var centres = new List<WorldPoint>();
            var answers = new List<string>();
            for (var step = 0; step < 6; step++)
            {
                world.AdvanceTo(world.Now.Plus(60_000));
                var centre = new WorldPoint(5_000 + strokes.NextDouble() * 4_000, 5_000 + strokes.NextDouble() * 4_000);
                var (radius, value) = (100 + strokes.NextDouble() * 1_200, strokes.NextDouble());
                Paint(world, centre.X, centre.Y, radius, value);
                world.Atlas.Paint(Water.Key, centre, radius, value);
                centres.Add(centre);
                foreach (var point in centres.SelectMany(c => new[] { c, new WorldPoint(c.X + 700, c.Y - 300) }))
                {
                    foreach (var level in Enum.GetValues<GridLevel>())
                    {
                        foreach (var field in new[] { Scratch, Gradient, Water.Key })
                        {
                            if (unloadBeforeEachQuery)
                            {
                                world.Atlas.Unload();
                            }

                            var sample = world.Atlas.Sample(field, point, level);
                            answers.Add($"{field} {level} {string.Join(",", sample.Values!)} {sample.ChangedAt}");
                        }
                    }
                }

                if (unloadBeforeEachQuery)
                {
                    world.Atlas.Unload();
                }

                var area = world.Atlas.Aggregate(Scratch, 4_000, 4_000, 10_000, 10_000);
                answers.Add($"{area.Mean} {area.Min} {area.Max}");
            }

            return answers;
        }

        Assert.Equal(Run(unloadBeforeEachQuery: true), Run(unloadBeforeEachQuery: false));
    }

    [Fact]
    public void AnswersNeverDependOnResidencyOrCacheBudget()
    {
        // SOV-P03-T07, T10, T17: a starved cache, a generous one and a full unload give identical answers.
        var points = Enumerable.Range(0, 20).Select(k =>
        {
            var random = RandomStream.For(9, "test.points", (ulong)k);
            return new WorldPoint(random.NextDouble() * 40_000, random.NextDouble() * 30_000);
        }).ToList();
        IReadOnlyList<string> Run(long budget, bool unloadBetween)
        {
            var world = NewWorld();
            world.Atlas.BudgetBytes = budget;
            Paint(world, 5_000, 5_000, 800, 0.7);
            Paint(world, 30_000, 20_000, 500, 0.2);
            var answers = new List<string>();
            foreach (var point in points)
            {
                foreach (var level in Enum.GetValues<GridLevel>())
                {
                    if (unloadBetween)
                    {
                        world.Atlas.Unload();
                    }

                    var scratch = world.Atlas.Sample(Scratch, point, level);
                    var gradient = world.Atlas.Sample(Gradient, point, level);
                    answers.Add($"{string.Join(",", scratch.Values!)} {string.Join(",", gradient.Values!)} {string.Join(";", scratch.Inputs.Select(input => input.Cell))}");
                }
            }

            answers.Add(world.ComputeChecksum());
            return answers;
        }

        var generous = Run(FieldAtlas.DefaultBudgetBytes, unloadBetween: false);
        Assert.Equal(generous, Run(1, unloadBetween: false));
        Assert.Equal(generous, Run(FieldAtlas.DefaultBudgetBytes, unloadBetween: true));
    }

    [Fact]
    public void ARemoteUnloadedCoordinateKeepsItsIdentityAfterReloadAndRefinement()
    {
        // SOV-P03-T17
        var world = NewWorld();
        var remote = new WorldPoint(39_000.3, 29_100.7);
        Paint(world, 39_000, 29_100, 30, 0.9);
        world.Submit(new DrawSurveyLine([new WorldPoint(38_000, 29_100), new WorldPoint(39_900, 29_100)], "far ridge"));
        var first = world.Atlas.Sample(Scratch, remote);
        var feature = Assert.Single(world.Inspect(remote, markerRadiusM: 50).NearbyFeatures).Feature.Id;

        world.Atlas.Unload();
        var reloaded = world.Atlas.Sample(Scratch, remote);
        var refined = world.Atlas.Sample(Scratch, remote, GridLevel.Tactical);

        Assert.Equal(Residency.Loaded, world.Atlas.ResidencyAt(Scratch, GridLevel.Local, remote));
        Assert.Equal(first.Values, reloaded.Values);
        Assert.Equal(first.Inputs.Select(input => (input.Cell, input.Values[0])), reloaded.Inputs.Select(input => (input.Cell, input.Values[0])));
        Assert.Equal(first.Values, refined.Values);
        Assert.Equal(feature, Assert.Single(world.Inspect(remote, markerRadiusM: 50).NearbyFeatures).Feature.Id);
    }

    [Fact]
    public void AreaSamplingMatchesPointSamplingAtEveryLevel()
    {
        var world = WorldWithWater();
        Paint(world, 1_000, 1_000, 300, 0.6);
        world.Atlas.Paint(Water.Key, new WorldPoint(1_000, 1_000), 300, 0.6);

        foreach (var field in new[] { Scratch, Gradient, Water.Key, "latitude" })
        {
            foreach (var level in Enum.GetValues<GridLevel>())
            {
                var components = field == Gradient ? 2 : 1;
                var area = world.Atlas.SampleArea(field, level, 600, 600, 50, 16, 16);
                for (var row = 0; row < 16; row++)
                {
                    for (var column = 0; column < 16; column++)
                    {
                        var point = world.Atlas.Sample(field, new WorldPoint(600 + (column + 0.5) * 50, 600 + (row + 0.5) * 50), level);
                        for (var c = 0; c < components; c++)
                        {
                            Assert.Equal(point.Values![c], area[((row * 16) + column) * components + c]);
                        }
                    }
                }
            }
        }
    }

    /// <summary>A base uniform within each local chunk: 0 west of x = 1 024 m, 0.5 east of it.</summary>
    private sealed class StepBase : IBaseSource
    {
        public void Fill(ChunkKey key, double[] values, int components) => Array.Fill(values, Value(key));

        public bool IsUniform(ChunkKey key, out IReadOnlyList<double> value)
        {
            value = [Value(key)];
            return true;
        }

        private static double Value(ChunkKey key) => key.X >= 1 ? 0.5 : 0;
    }
}

using System.Globalization;
using Sovereigns.Client.Ui;
using Sovereigns.Client.View;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Client.Tests;

/// <summary>What the atlas overlays, legend, level choice and inspector say about a painted world, without a scene (SOV-P03-T13, T14).</summary>
public static class AtlasViewTests
{
    public static IEnumerable<TestCase> Cases() =>
    [
        new("atlas view: the overlay image is the grey mapping of SampleArea values, no data is transparent, north is up", OverlayImageMatchesSamples),
        new("atlas view: a field far narrower than its range is mapped over the data range and says so", NarrowFieldUsesDataRange),
        new("atlas view: gradient arrows follow scratch_gradient toward higher values, one per nonzero sample", ArrowsFollowGradient),
        new("atlas view: the zoom chooses the level closest to, and not finer than, the sample spacing", LevelFollowsZoom),
        new("atlas view: layers cover every catalogue field by family, available ones first, unavailable ones marked", LayerList),
        new("atlas view: resident chunks become distinct outlines, coarse first, coloured by content", ChunkOutlineList),
        new("atlas view: the legend names level and range, vectors give an arrow scale, and unavailable layers say No data", LegendText),
        new("atlas view: the inspector lists level, classification, basis, vector components, grid inputs with weights, and nearby features", InspectorListsInputs),
        new("atlas view: tool state steps through 1-2-5 radii and 0.1 values, and builds the survey command from the pending points", ToolState),
    ];

    private static World PaintedWorld()
    {
        var world = TestWorlds.LaunchFixture().World!;
        Check.True(world.Submit(new PaintScratch(new WorldPoint(300, 300), 150, 1.0)).Rejection is null, "first stroke accepted");
        Check.True(world.Submit(new PaintScratch(new WorldPoint(380, 300), 60, 0.35)).Rejection is null, "second stroke accepted");
        return world;
    }

    private static (byte R, byte G, byte B, byte A) Bytes(Color colour) =>
        ((byte)Math.Round(colour.R * 255), (byte)Math.Round(colour.G * 255), (byte)Math.Round(colour.B * 255), (byte)Math.Round(colour.A * 255));

    private static Task OverlayImageMatchesSamples()
    {
        var world = PaintedWorld();
        var scratch = FieldCatalog.Scratch;

        // The area crosses the west and south edges of the world, so some samples have no data.
        var overlay = FieldOverlay.Sample(world.Atlas, scratch, GridLevel.Local, (-200, -100, 800, 600), 8);
        var image = overlay.ToImage();
        Check.Equal(overlay.Columns, image.GetWidth(), "one pixel per sample column");
        Check.Equal(overlay.Rows, image.GetHeight(), "one pixel per sample row");
        Check.True(!overlay.UsesDataRange && overlay.RangeMin == 0 && overlay.RangeMax == 1, "a field spanning its range is mapped over the declared range");

        var values = world.Atlas.SampleArea(scratch.Key, GridLevel.Local, overlay.West, overlay.South, overlay.SpacingM, overlay.Columns, overlay.Rows);
        var transparent = 0;
        for (var row = 0; row < overlay.Rows; row++)
        {
            for (var column = 0; column < overlay.Columns; column++)
            {
                var value = values[(row * overlay.Columns) + column];
                var expected = FieldRamp.Map(value, 0, 1);
                var pixel = Bytes(image.GetPixel(column, overlay.Rows - 1 - row));
                Check.Equal(expected, pixel, $"pixel of column {column}, row {row} (value {value})");
                if (double.IsNaN(value))
                {
                    transparent++;
                    Check.Equal<byte>(0, pixel.A, "no data is fully transparent");
                }
                else
                {
                    Check.Equal<byte>(255, pixel.A, "data is opaque");
                }
            }
        }

        Check.True(transparent > 0 && transparent < overlay.SampleCount, $"some, not all, samples are outside the world ({transparent} of {overlay.SampleCount})");
        Check.Equal(transparent, overlay.NoDataCount, "the overlay counts its no-data samples");

        var centre = image.GetPixel((int)((300 - overlay.West) / overlay.SpacingM), overlay.Rows - 1 - (int)((300 - overlay.South) / overlay.SpacingM));
        var unpainted = image.GetPixel((int)((700 - overlay.West) / overlay.SpacingM), overlay.Rows - 1 - (int)((500 - overlay.South) / overlay.SpacingM));
        Check.True(centre.R > unpainted.R + 0.5f, "the painted centre (1.0) is much lighter than unpainted ground (0)");
        Check.True(unpainted.A > 0.99f && unpainted.R > 0.1f, "an unpainted value is dark grey, not transparent and not black");
        return Task.CompletedTask;
    }

    private static Task NarrowFieldUsesDataRange()
    {
        var world = TestWorlds.LaunchFixture().World!;
        var overlay = FieldOverlay.Sample(world.Atlas, FieldCatalog.Latitude, GridLevel.Regional, (0, 0, 100_000, 100_000), 4096);
        Check.True(overlay.UsesDataRange, "latitude over 100 km spans a tiny part of -90 to 90 degrees");
        Check.True(overlay.RangeMin == overlay.DataMin && overlay.RangeMax == overlay.DataMax, "the ramp spans the data range");
        Check.True(overlay.DataMax - overlay.DataMin < 0.1 * Math.PI * 0.05, "the data range is narrow");

        var image = overlay.ToImage();
        // The last sample row lies beyond the world's north edge (25 rows of 4096 m cover 102.4 km), so it has no data; the next one is the northernmost with a value.
        var north = Bytes(image.GetPixel(2, 1));
        var south = Bytes(image.GetPixel(2, overlay.Rows - 1));
        Check.True(north.R > south.R, "latitude grows northward, so the top row is lighter than the bottom row");
        Check.Equal(FieldRamp.Map(overlay.DataMin, overlay.DataMin, overlay.DataMax).R, south.R, "the smallest value is the dark end");

        var flat = FieldOverlay.Sample(world.Atlas, FieldCatalog.Scratch, GridLevel.Local, (1000, 1000, 2000, 2000), 100);
        Check.True(!flat.UsesDataRange && flat.RangeMin == 0 && flat.RangeMax == 1, "a constant field keeps its declared range instead of a degenerate one");
        var flatPixel = Bytes(flat.ToImage().GetPixel(0, 0));
        Check.Equal(FieldRamp.Map(0, 0, 1), flatPixel, "unpainted scratch is the dark end of the declared range");

        var unavailable = FieldOverlay.Sample(world.Atlas, FieldCatalog.Get("elevation"), GridLevel.Regional, (0, 0, 10_000, 10_000), 1000);
        Check.True(!unavailable.HasData && unavailable.NoDataCount == unavailable.SampleCount, "an unavailable field samples as no data everywhere");
        Check.True(Bytes(unavailable.ToImage().GetPixel(0, 0)).A == 0, "and draws as transparent");
        return Task.CompletedTask;
    }

    private static Task ArrowsFollowGradient()
    {
        var world = TestWorlds.LaunchFixture().World!;
        var centre = new WorldPoint(50_000, 50_000);

        // A cone: rings 20 m wide whose value rises toward the centre, so the gradient points inward everywhere inside it.
        for (var radius = 400.0; radius >= 20; radius -= 20)
        {
            Check.True(world.Submit(new PaintScratch(centre, radius, Math.Round(1 - (radius / 420), 3))).Rejection is null, $"ring {radius} accepted");
        }

        var overlay = FieldOverlay.Sample(world.Atlas, FieldCatalog.ScratchGradient, GridLevel.Local, (49_400, 49_400, 50_600, 50_600), 50);
        Check.True(overlay.IsVector, "the gradient is a vector field");
        Check.Equal(576, overlay.SampleCount, "a 24 by 24 grid of 50 m samples");

        var nonzero = 0;
        for (var row = 0; row < overlay.Rows; row++)
        {
            for (var column = 0; column < overlay.Columns; column++)
            {
                var point = new WorldPoint(overlay.West + ((column + 0.5) * overlay.SpacingM), overlay.South + ((row + 0.5) * overlay.SpacingM));
                var sample = world.Atlas.Sample(FieldCatalog.ScratchGradient.Key, point, GridLevel.Local);
                if (sample.Values![0] != 0 || sample.Values[1] != 0)
                {
                    nonzero++;
                }
            }
        }

        var arrows = overlay.Arrows();
        Check.Equal(nonzero, arrows.Count, "one arrow per sample with a nonzero gradient");
        Check.True(arrows.Count >= 100 && arrows.Count < overlay.SampleCount, $"the cone fills the middle of the grid, not all of it ({arrows.Count})");

        foreach (var arrow in arrows)
        {
            var truth = world.Atlas.Sample(FieldCatalog.ScratchGradient.Key, arrow.Origin, GridLevel.Local).Values!;
            Check.Near(truth[0], arrow.East, 1e-12, $"east component at {arrow.Origin}");
            Check.Near(truth[1], arrow.North, 1e-12, $"north component at {arrow.Origin}");
            var towardCentre = ((centre.X - arrow.Origin.X) * arrow.East) + ((centre.Y - arrow.Origin.Y) * arrow.North);
            Check.True(towardCentre > 0, $"the gradient at {arrow.Origin} points toward the higher values at the centre");
        }

        var west = arrows.First(arrow => Math.Abs(arrow.Origin.X - 49_625) < 1 && Math.Abs(arrow.Origin.Y - 50_025) < 1);
        Check.True(west.East > 0 && Math.Abs(west.North) < west.East, "west of the cone the arrow points east");
        var south = arrows.First(arrow => Math.Abs(arrow.Origin.X - 50_025) < 1 && Math.Abs(arrow.Origin.Y - 49_625) < 1);
        Check.True(south.North > 0 && Math.Abs(south.East) < south.North, "south of the cone the arrow points north");

        var largest = arrows.Max(arrow => arrow.Magnitude);
        Check.Near(largest, overlay.DataMax, 1e-12, "the data maximum is the largest arrow");
        Check.True(overlay.ArrowReference >= largest && overlay.ArrowReference < largest * 2.5 + 1e-12, "a full-length arrow stands for the next 1-2-5 value above the largest");
        Check.Equal(0.05, FieldOverlay.NiceCeiling(0.031), "1-2-5 rounding up");
        Check.Equal(2.0, FieldOverlay.NiceCeiling(2.0), "an exact value stays");
        Check.Equal(0.0, FieldOverlay.NiceCeiling(0), "zero has no scale");

        var flat = FieldOverlay.Sample(world.Atlas, FieldCatalog.ScratchGradient, GridLevel.Local, (10_000, 10_000, 11_000, 11_000), 100);
        Check.Equal(0, flat.Arrows().Count, "a flat area has no arrows");
        Check.Equal(0.0, flat.ArrowReference, "and no arrow scale");

        var macro = FieldOverlay.Sample(world.Atlas, FieldCatalog.ScratchGradient, GridLevel.Macro, (49_400, 49_400, 50_600, 50_600), 100);
        Check.True(macro.DataMax < overlay.DataMax / 4, "at macro level the cone's gradient is averaged out; arrows are meaningful at local level");
        return Task.CompletedTask;
    }

    private static Task LevelFollowsZoom()
    {
        Check.Equal(GridLevel.Tactical, ViewLevels.ForSpacing(0.25), "spacing finer than 1 m never goes below tactical");
        Check.Equal(GridLevel.Tactical, ViewLevels.ForSpacing(1), "1 m is the tactical cell");
        Check.Equal(GridLevel.Local, ViewLevels.ForSpacing(1.01), "just coarser than 1 m needs the local level");
        Check.Equal(GridLevel.Local, ViewLevels.ForSpacing(16), "16 m is the local cell");
        Check.Equal(GridLevel.Regional, ViewLevels.ForSpacing(16.5), "just coarser than 16 m");
        Check.Equal(GridLevel.Regional, ViewLevels.ForSpacing(256), "256 m is the regional cell");
        Check.Equal(GridLevel.Macro, ViewLevels.ForSpacing(300), "coarser than regional cells");
        Check.Equal(GridLevel.Macro, ViewLevels.ForSpacing(50_000), "far coarser stays at macro");

        Check.Equal(GridLevel.Tactical, ViewLevels.ForZoom(ViewTransform.MaxPixelsPerMetre), "fully zoomed in (6 px per sample is 0.3 m)");
        Check.Equal(GridLevel.Local, ViewLevels.ForZoom(0.5), "0.5 px/m: a sample every 12 m");
        Check.Equal(GridLevel.Regional, ViewLevels.ForZoom(0.1), "0.1 px/m: a sample every 60 m");
        Check.Equal(GridLevel.Macro, ViewLevels.ForZoom(0.0072), "a fitted 100 km world: a sample every 833 m");

        Check.Equal("local (16 m)", ViewLevels.Describe(GridLevel.Local), "level text with its cell size");
        Check.Equal("macro (4.096 km)", ViewLevels.Describe(GridLevel.Macro), "coarse cell sizes in kilometres");
        Check.Equal(GridLevel.Tactical, ViewLevels.Step(GridLevel.Local, 1), "one step finer");
        Check.Equal(GridLevel.Regional, ViewLevels.Step(GridLevel.Local, -1), "one step coarser");
        Check.Equal(null, ViewLevels.Step(GridLevel.Tactical, 1), "past tactical is automatic");
        Check.Equal(null, ViewLevels.Step(GridLevel.Macro, -1), "past macro is automatic");
        Check.True(ViewLevels.TryParse("Regional", out var level) && level == GridLevel.Regional && !ViewLevels.TryParse("2", out _) && !ViewLevels.TryParse("fine", out _), "levels parse by name only");
        return Task.CompletedTask;
    }

    private static Task LayerList()
    {
        var world = TestWorlds.LaunchFixture().World!;
        var layers = MapLayers.For(world.Atlas);
        Check.Equal(2 + FieldCatalog.All.Count, layers.Count, "two base layers plus every catalogue field");
        Check.Equal(MapLayer.WorldFrameKey, layers[0].Key, "world frame first");
        Check.Equal(MapLayer.DeveloperMarkersKey, layers[1].Key, "developer markers second");
        foreach (var field in FieldCatalog.All)
        {
            var layer = layers.SingleOrDefault(candidate => candidate.Key == field.Key) ?? throw new CheckFailedException($"no layer for field {field.Key}");
            Check.Equal(world.Atlas.IsAvailable(field.Key), layer.IsAvailable, $"availability of {field.Key}");
            if (layer.IsAvailable)
            {
                Check.Equal(field.Name, layer.ListText, $"available {field.Key} is listed by name");
                Check.True(layer.IsFieldOverlay, $"{field.Key} is drawn as an overlay");
            }
            else
            {
                Check.Equal($"{field.Name} (unavailable — {field.Owner}, {field.ImplementedBy})", layer.ListText, $"unavailable {field.Key} is marked with owner and phase");
                Check.Contains(layer.Legend, "No data — unavailable:", $"{field.Key} legend");
                Check.True(!layer.IsFieldOverlay, $"{field.Key} has no overlay");
            }
        }

        Check.Equal(4, layers.Count(layer => layer.IsFieldOverlay), "four fields are drawable: latitude, solar time, scratch and its gradient");

        var families = layers.Skip(2).Select(layer => layer.Family!.Key).ToList();
        Check.Equal(FieldFamilies.All.Count + 1, families.Distinct().Count(), "every family, and the developer tools, appears");
        Check.Equal(families.Distinct().Count(), families.Where((key, index) => index == 0 || key != families[index - 1]).Count(), "each family's fields are listed together");
        var firstUnavailable = layers.ToList().FindIndex(layer => !layer.IsAvailable);
        Check.True(layers.Skip(firstUnavailable).All(layer => !layer.IsAvailable), "families with drawable fields come first");
        Check.Equal(FieldFamilies.GeographicReference.Key, layers[2].Family!.Key, "geographic reference leads");
        Check.Equal(FieldCatalog.Developer.Key, layers[4].Family!.Key, "then the developer tools, which the catalogue lists last");

        Check.Equal(4, MapLayers.IndexOf(layers, "Developer scratch"), "a layer is found by name, case-insensitively");
        Check.Equal(layers.ToList().FindIndex(layer => layer.Key == "scratch_gradient"), MapLayers.IndexOf(layers, "SCRATCH_GRADIENT"), "or by key");
        Check.Equal(-1, MapLayers.IndexOf(layers, "no such layer"), "an unknown layer is not found");
        return Task.CompletedTask;
    }

    private static Task ChunkOutlineList()
    {
        var world = PaintedWorld();
        world.Atlas.Sample("scratch_gradient", new WorldPoint(300, 300), GridLevel.Local);
        world.Atlas.Sample("scratch", new WorldPoint(300, 300), GridLevel.Regional);
        world.Atlas.Sample("scratch_gradient", new WorldPoint(300, 300), GridLevel.Regional);
        world.Atlas.Sample("scratch", new WorldPoint(300, 300), GridLevel.Macro);
        var resident = world.Atlas.ResidentChunks();
        var outlines = ChunkOutlines.From(resident);
        Check.True(outlines.Count < resident.Count, "the regional aggregate chunk of scratch and of its gradient share one outline");
        Check.Equal(resident.Select(chunk => (chunk.Key, chunk.Content)).Distinct().Count(), outlines.Count, "one outline per distinct chunk and content");
        Check.True(outlines.Select(outline => outline.Content).Distinct().Count() >= 3, "native, derived and aggregate content are all resident");
        Check.True(outlines.Zip(outlines.Skip(1)).All(pair => pair.First.Key.Level <= pair.Second.Key.Level), "coarse levels come first");
        Check.Equal(4, ChunkOutlines.Contents.Count, "four kinds of content");
        Check.Equal(4, ChunkOutlines.Contents.Select(ChunkOutlines.ColourOf).Distinct().Count(), "each kind has its own colour");
        Check.Equal("aggregate", ChunkOutlines.NameOf(ChunkContent.Aggregate), "content names");
        Check.Equal(0, ChunkOutlines.From([]).Count, "nothing resident, nothing to outline");
        return Task.CompletedTask;
    }

    private static Task LegendText()
    {
        var world = PaintedWorld();
        var layers = MapLayers.For(world.Atlas);
        var scratch = layers.Single(layer => layer.Key == "scratch");
        var overlay = FieldOverlay.Sample(world.Atlas, FieldCatalog.Scratch, GridLevel.Local, (-200, -100, 800, 600), 8);
        var legend = LegendContent.Build(scratch, 0, overlay, GridLevel.Local, levelFixed: false);
        Check.Contains(legend.Text, "Developer scratch [1]", "field name and unit");
        Check.Contains(legend.Text, "level: local (16 m) (chosen from the zoom); one sample every 8 m", "level, how it was chosen and the sample spacing");
        Check.Contains(legend.Text, "grey ramp spans the field range: dark 0, light 1", "range");
        Check.Contains(legend.Text, "no data: hatched outside the world; transparent where a sample has no value", "no-data meaning");
        Check.True(legend.HasBar && legend.MinText == "0" && legend.MaxText == "1" && legend.ShowsNoData, "colour bar from 0 to 1 and a no-data swatch");
        Check.Contains(LegendContent.Build(scratch, 0, overlay, GridLevel.Regional, levelFixed: true).Text, "level: regional (256 m) (fixed with the level keys)", "a fixed level says so");

        var latitudeLayer = layers.Single(layer => layer.Key == "latitude");
        var latitude = LegendContent.Build(latitudeLayer, 0, FieldOverlay.Sample(world.Atlas, FieldCatalog.Latitude, GridLevel.Macro, (0, 0, 100_000, 100_000), 4096), GridLevel.Macro, false);
        Check.Contains(latitude.Text, "grey ramp spans the data range in view, not the field range (-90.0000° to 90.0000°)", "a narrow field names its data range");
        Check.True(latitude.MinText.EndsWith('°') && latitude.MaxText.EndsWith('°'), "latitude is shown in degrees");

        var gradientLayer = layers.Single(layer => layer.Key == "scratch_gradient");
        var painted = FieldOverlay.Sample(world.Atlas, FieldCatalog.ScratchGradient, GridLevel.Local, (-200, -100, 800, 600), 40);
        var vector = LegendContent.Build(gradientLayer, 0, painted, GridLevel.Local, false);
        Check.True(!vector.HasBar && vector.ArrowText.EndsWith("m-1", StringComparison.Ordinal), $"a vector overlay has an arrow scale in m-1: '{vector.ArrowText}'");
        Check.Contains(vector.Text, "a full-length arrow is", "the arrow scale is explained");
        var flat = LegendContent.Build(gradientLayer, 0, FieldOverlay.Sample(world.Atlas, FieldCatalog.ScratchGradient, GridLevel.Local, (10_000, 10_000, 11_000, 11_000), 100), GridLevel.Local, false);
        Check.Contains(flat.Text, "flat: every vector in view is 0 m-1", "a flat vector field says so");

        var stale = LegendContent.Build(gradientLayer, 0, null, GridLevel.Local, false);
        Check.Contains(stale.Text, "no samples", "no overlay is stated, not guessed");
        var elevation = layers.Single(layer => layer.Key == "elevation");
        var unavailable = LegendContent.Build(elevation, 0, null, GridLevel.Local, false);
        Check.Contains(unavailable.Text, "No data — unavailable: World foundation implements elevation above datum in Phase 04", "an unavailable layer names owner and phase");
        Check.True(!unavailable.HasBar && !unavailable.ShowsNoData, "and has no colour bar");
        var markers = LegendContent.Build(layers[1], 3, null, GridLevel.Local, false);
        Check.Contains(markers.Text, "3 marker(s) in the world.", "the marker layer counts markers");
        return Task.CompletedTask;
    }

    private static Task InspectorListsInputs()
    {
        var world = TestWorlds.LaunchFixture().World!;
        var centre = new WorldPoint(50_000, 50_000);
        world.Submit(new PaintScratch(centre, 300, 0.6));
        world.Submit(new PaintScratch(centre, 100, 0.9));
        world.AdvanceTo(world.Now.Plus(60_000));
        world.Submit(new PaintScratch(new WorldPoint(50_050, 50_000), 20, 0.2));
        world.Submit(new DrawSurveyLine([new WorldPoint(50_050, 50_040), new WorldPoint(50_250, 50_040)], "S1"));

        var inspection = world.Inspect(new WorldPoint(50_150, 50_000), 100, GridLevel.Local);
        Check.Equal(1, inspection.NearbyFeatures.Count, "the survey line is within 100 m");
        var lines = InspectorContent.Build(inspection, 100, "Inspection level: local (16 m)");
        var text = lines.Select(line => line.Text).ToList();
        Check.Equal("Inspection level: local (16 m)", text[2], "the level note sits under the location");

        var reading = text.IndexOf("  developer scratch: 0.6 (dimensionless)");
        Check.True(reading >= 0, $"the scratch value shows with its unit:\n{string.Join('\n', text)}");
        Check.Equal("      level: local (16 m)", text[reading + 1], "level");
        Check.Equal("      classification: stored: bilinear over native cells with overrides", text[reading + 2], "classification");
        Check.Equal("      basis: stored", text[reading + 3], "basis");
        Check.Equal("      owner: Developer tools", text[reading + 4], "owner");
        Check.Equal("      source: base data and stored writes of scratch", text[reading + 5], "source");
        Check.Equal("      updated: 1 Mar 1203, 06:00:00", text[reading + 6], "the painted cells changed at the start");
        Check.Equal("      inputs (4 grid cell(s) and weights):", text[reading + 7], "input header");
        var weight = 0.0;
        for (var i = 8; i < 12; i++)
        {
            Check.True(System.Text.RegularExpressions.Regex.IsMatch(text[reading + i], @"^ {8}cell local\[\d+,\d+\] w=\d\.\d{3} v=\d\.\d{3} \((stored|base)\)$"), $"input line format: '{text[reading + i]}'");
            weight += double.Parse(text[reading + i].Split("w=")[1].Split(' ')[0], CultureInfo.InvariantCulture);
        }

        Check.Near(1, weight, 0.004, "the four weights add up to one");
        Check.True(text.Skip(reading + 8).Take(4).Any(line => line.EndsWith("v=0.600 (stored)", StringComparison.Ordinal)), "a painted cell is a stored input");

        var gradient = text.FindIndex(line => line.StartsWith("  developer scratch gradient: ", StringComparison.Ordinal) && line.EndsWith("m-1 (magnitude)", StringComparison.Ordinal));
        Check.True(gradient >= 0, "the vector reading shows its magnitude with the unit");
        Check.Equal("      classification: derived: bilinear over central differences of scratch", text[gradient + 2], "vector classification");
        Check.True(text[gradient + 7].StartsWith("      east: ", StringComparison.Ordinal) && text[gradient + 7].EndsWith(" m-1", StringComparison.Ordinal), "east component");
        Check.True(text[gradient + 8].StartsWith("      north: ", StringComparison.Ordinal) && text[gradient + 8].EndsWith(" m-1", StringComparison.Ordinal), "north component");
        Check.Contains(text[gradient + 10], "(derived)", "vector inputs are derived values");
        Check.Contains(text[gradient + 10], "v=(", "vector inputs show both components");

        Check.True(text.Contains("Nearby features within 100 m"), "features have their own section");
        Check.Contains(text.Single(line => line.StartsWith("  survey:1 ", StringComparison.Ordinal)), "\"S1\": survey line, 40 m away, drawn 1 Mar 1203, 06:01:00", "id, label, kind, distance and time");

        var later = InspectorContent.Build(world.Inspect(new WorldPoint(50_050, 50_000), 100, GridLevel.Local), 100).Select(line => line.Text).ToList();
        var repainted = later.IndexOf("  developer scratch: 0.2 (dimensionless)");
        Check.True(repainted >= 0, "inside the later stroke the value is its 0.2");
        Check.Equal("      updated: 1 Mar 1203, 06:01:00", later[repainted + 6], "a reading shows when its cells last changed");

        var elevation = lines.Single(line => line.Text.StartsWith("  elevation above datum:", StringComparison.Ordinal));
        Check.True(elevation.Style == LineStyle.Dim, "an unavailable reading stays grey");
        Check.True(text.Contains("  developer scratch: 0.6 (dimensionless)") && lines.First(line => line.Text.StartsWith("Developer tools", StringComparison.Ordinal)).Style == LineStyle.Heading, "the developer family is listed as available");

        var macro = InspectorContent.Build(world.Inspect(new WorldPoint(50_150, 50_000), 100, GridLevel.Macro), 100).Select(line => line.Text).ToList();
        var aggregate = macro.FindIndex(line => line.StartsWith("  developer scratch: ", StringComparison.Ordinal));
        Check.Equal("      level: macro (4.096 km)", macro[aggregate + 1], "the same point at macro level");
        Check.Contains(macro[aggregate + 2], "classification: aggregate: bilinear over macro cell means", "an aggregate says so");
        Check.True(macro.Skip(aggregate + 8).Take(4).All(line => line.Contains("cell macro[", StringComparison.Ordinal) && line.EndsWith("(aggregate)", StringComparison.Ordinal)), "its inputs are macro cells");

        var none = InspectorContent.Build(world.Inspect(new WorldPoint(10, 10), 5, GridLevel.Local), 5).Select(line => line.Text).ToList();
        Check.True(none.Contains("Nearby features within 5 m: none") && none.Contains("Markers within 5 m: none"), "no features or markers nearby");
        return Task.CompletedTask;
    }

    private static Task ToolState()
    {
        var tools = new DeveloperTools();
        Check.Equal(500.0, tools.RadiusM, "default radius");
        Check.Equal(1.0, tools.Value, "default value");
        Check.Equal("paint: radius 500 m, value 1.00   |   survey line: none pending", tools.Describe(), "default status text");
        Check.True(DeveloperTools.Radii[^1] <= PaintScratch.MaxRadiusM && DeveloperTools.Radii.Zip(DeveloperTools.Radii.Skip(1)).All(pair => pair.First < pair.Second), "radii ascend and respect the world limit");

        Check.True(tools.StepRadius(1) && tools.RadiusM == 1000, "radius up");
        Check.True(tools.StepRadius(1) && tools.StepRadius(1) && tools.RadiusM == 5000, "radius up to the limit");
        Check.True(!tools.StepRadius(1) && tools.RadiusM == 5000, "radius stops at the limit");
        while (tools.StepRadius(-1))
        {
        }

        Check.Equal(10.0, tools.RadiusM, "radius stops at the smallest");

        Check.True(!tools.StepValue(1), "value starts at the maximum");
        for (var i = 0; i < 3; i++)
        {
            tools.StepValue(-1);
        }

        Check.Equal(0.7, tools.Value, "values step by exactly 0.1 without drift");
        while (tools.StepValue(-1))
        {
        }

        Check.Equal(0.0, tools.Value, "value stops at 0, which erases paint");
        tools.StepValue(1);
        Check.Equal(new PaintScratch(new WorldPoint(1, 2), 10, 0.1), tools.Paint(new WorldPoint(1, 2)), "the paint command carries the brush");

        Check.True(tools.Finish() is null, "no line without points");
        Check.True(tools.AddPoint(new WorldPoint(10, 10)) && tools.Finish() is null, "one point is not a line");
        Check.True(!tools.AddPoint(new WorldPoint(10, 10)), "a repeated point is not added");
        Check.True(tools.AddPoint(new WorldPoint(20, 30)) && tools.AddPoint(new WorldPoint(50, 30)), "more points");
        Check.Contains(tools.Describe(), "survey line: 3 pending point(s), next label S1", "pending line in the status text");
        var command = tools.Finish() ?? throw new CheckFailedException("three points make a line");
        Check.Equal("S1", command.Label, "first label");
        Check.Equal(3, command.Points.Count, "all points");
        Check.Equal(3, tools.Pending.Count, "the line stays pending until the world accepts it");
        tools.Accepted();
        Check.Equal(0, tools.Pending.Count, "accepted: nothing pending");
        Check.Equal("S2", tools.NextLabel, "next label");
        tools.AddPoint(new WorldPoint(1, 1));
        tools.Cancel();
        Check.Equal(0, tools.Pending.Count, "cancel drops the pending points");
        Check.Equal("S2", tools.NextLabel, "cancel does not use up a label");
        return Task.CompletedTask;
    }
}

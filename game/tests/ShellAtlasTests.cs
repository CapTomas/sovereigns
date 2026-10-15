using Sovereigns.Client.Controls;
using Sovereigns.Client.Ui;
using Sovereigns.Client.View;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Scenarios;

namespace Sovereigns.Client.Tests;

/// <summary>The atlas overlays, residency overlay, HUD chunk lines and developer tools driven through the real shell (SOV-P03-T13, T14).</summary>
public sealed partial class ShellTests
{
    private IEnumerable<TestCase> AtlasCases() =>
    [
        new("shell atlas: the scratch overlay draws, follows zoom to a level, and the level keys fix it for overlay and inspector", OverlayFollowsZoomAndLevelKeys),
        new("shell atlas: gradient arrows are drawn for the painted ramp and the legend gives their scale", ArrowsAreDrawn),
        new("shell atlas: the residency overlay outlines the resident chunks and the HUD shows the active chunk and cache statistics", ResidencyAndHud),
        new("shell atlas: paint and survey tools journal their commands, show their state in the status line, and replay to the same checksum", ToolsAreJournaled),
        new("shell atlas: the inspector shows level, classification and grid inputs for the selected scratch point and highlights the nearest survey line", InspectorShowsInputs),
        new("shell atlas: --paint, --line, --center, --zoom and --level start a reproducible view", LaunchOptionsReproduceView),
    ];

    private async Task OverlayFollowsZoomAndLevelKeys()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--layer", "scratch", "--paint", "50000,50000,600,1", "--center", "50000,50000", "--zoom", "2", "--no-hud");
        try
        {
            var shell = main.Shell!;
            var map = shell.Map;
            Check.True(!shell.Hud.Visible, "--no-hud hides the HUD");
            Check.Equal("scratch", shell.ActiveLayer.Key, "--layer takes a field key");
            Check.Equal(GridLevel.Local, shell.ViewLevel, "2 m/px: a sample every 12 m is local");
            Check.True(map.Overlay is { IsVector: false } overlay && overlay.Level == GridLevel.Local && overlay.Field.Key == "scratch", "a scalar overlay was sampled at local level");
            Check.True(map.LastDraw.OverlayTexture, "its picture was drawn");
            Check.Equal(CanvasItem.TextureFilterEnum.Nearest, map.TextureFilter, "without smoothing between samples");
            Check.Contains(shell.Legend.Text, "level: local (16 m) (chosen from the zoom)", "the legend shows the level");
            Check.Contains(shell.Legend.Text, "Developer scratch [1]", "and the field");

            // The overlay is what the atlas answers: the painted centre is the top of the range.
            var centre = map.Overlay!.ScalarAt((int)((50_000 - map.Overlay.West) / map.Overlay.SpacingM), (int)((50_000 - map.Overlay.South) / map.Overlay.SpacingM));
            Check.Near(1.0, centre, 1e-9, "the centre of the painted disc reads 1");

            map.SetView(new WorldPoint(50_000, 50_000), 10);
            map.RefreshOverlay(force: true);
            Check.Equal(GridLevel.Regional, shell.ViewLevel, "10 m/px: a sample every 60 m is regional");
            Check.Equal(GridLevel.Regional, map.Overlay!.Level, "and the overlay is sampled there");
            map.SetView(new WorldPoint(50_000, 50_000), 150);
            map.RefreshOverlay(force: true);
            Check.Equal(GridLevel.Macro, shell.ViewLevel, "150 m/px: a sample every 900 m is macro");
            var macroCentre = map.Overlay!.ScalarAt(map.Overlay.Columns / 2, map.Overlay.Rows / 2);
            Check.True(macroCentre < 0.2, $"the aggregate of a 600 m disc over 4 km cells is much fainter than the stored 1 ({macroCentre})");

            map.SetView(new WorldPoint(50_000, 50_000), 2);
            Press(Key.Pagedown);
            Check.Equal(GridLevel.Tactical, shell.ViewLevel, "Page Down from automatic local fixes tactical");
            Check.Equal(GridLevel.Tactical, map.LevelOverride, "the level is fixed");
            Check.Contains(shell.Status.ToolsText, "view level: tactical (1 m), fixed", "the status line shows the fixed level");
            Press(Key.Pagedown);
            Check.True(map.LevelOverride is null, "past tactical is automatic again");
            Check.Equal(GridLevel.Local, shell.ViewLevel, "which the zoom chooses");
            Press(Key.Pageup);
            Check.Equal(GridLevel.Regional, shell.ViewLevel, "Page Up fixes the next coarser level");
            Check.Contains(shell.Status.ToolsText, "view level: regional (256 m), fixed", "the status line shows it");

            shell.SelectLocation(new WorldPoint(50_000, 50_000));
            Check.Equal(GridLevel.Regional, shell.InspectionLevel, "the inspector follows the view level");
            Check.True(shell.Inspector.Current!.Families.SelectMany(family => family.Readings).Where(reading => reading.Level is not null).All(reading => reading.Level == GridLevel.Regional), "every atlas reading was sampled at regional level");
            Check.Contains(string.Join('\n', shell.Inspector.Lines.Select(line => line.Text)), "Inspection level: regional (256 m) (fixed;", "the inspector says which level it used");

            shell.SetLayer(0);
            Press(Key.Pageup);
            Check.Equal(GridLevel.Macro, map.LevelOverride, "one more step coarser is macro");
            Press(Key.Pageup);
            Check.True(map.LevelOverride is null, "automatic again after macro");
            Check.True(shell.InspectionLevel is null, "with no field layer and no fixed level the inspector uses each field's native level");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task ArrowsAreDrawn()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--layer", "scratch_gradient", "--center", "50000,50000", "--zoom", "2",
            "--paint", "50000,50000,400,0.25;50000,50000,300,0.5;50000,50000,200,0.75;50000,50000,100,1");
        try
        {
            var shell = main.Shell!;
            var map = shell.Map;
            var overlay = map.Overlay ?? throw new CheckFailedException("a vector overlay was sampled");
            Check.True(overlay.IsVector && overlay.Level == GridLevel.Local, "gradient arrows at local level");
            Check.True(overlay.SpacingM >= 70 && overlay.SpacingM <= 90, $"arrows are about 40 px apart at 2 m/px ({overlay.SpacingM} m)");
            Check.True(map.Overlay!.Arrows().Count > 10, "the cone has arrows");
            Check.Equal(map.Overlay.Arrows().Count, map.LastDraw.Arrows, "every arrow in view was drawn");
            Check.True(!map.LastDraw.OverlayTexture, "a vector field has no picture, only arrows");
            Check.True(map.Overlay.Arrows().All(arrow => ((50_000 - arrow.Origin.X) * arrow.East) + ((50_000 - arrow.Origin.Y) * arrow.North) > 0), "every arrow points toward the higher values");
            Check.Contains(shell.Legend.Text, "a full-length arrow is", "the legend gives the scale");
            Check.True(shell.Legend.Model!.ArrowText.EndsWith("m-1", StringComparison.Ordinal), "in the field's unit");
            Check.True(!shell.Legend.Model.HasBar, "no colour bar for arrows");

            shell.Dispatch(InputActions.CameraRecenter);
            map.RefreshOverlay(force: true);
            Check.Equal(GridLevel.Macro, shell.ViewLevel, "fitted: macro level");
            Check.True(map.Overlay!.DataMax < 1e-3, "the macro average of the cone's gradient is almost nothing; arrows need local level");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task ResidencyAndHud()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--layer", "scratch_gradient", "--center", "50000,50000", "--zoom", "2", "--paint", "50000,50000,300,1");
        try
        {
            var shell = main.Shell!;
            var map = shell.Map;
            var world = main.Host!.World;
            Check.True(!shell.ResidencyVisible && map.LastDraw.ChunkOutlines == 0, "the residency overlay is off by default");

            shell.Dispatch(InputActions.ToggleResidency);
            await runner.Frames(2);
            Check.True(shell.ResidencyVisible, "C turns it on");
            var resident = world.Atlas.ResidentChunks();
            Check.True(resident.Count > 0, "sampling the gradient left chunks resident");
            var bounds = map.Transform.VisibleBounds();
            var visible = ChunkOutlines.From(resident).Count(outline =>
            {
                var (west, south, east, north) = outline.Key.Bounds;
                return east >= bounds.MinX && west <= bounds.MaxX && north >= bounds.MinY && south <= bounds.MaxY;
            });
            Check.True(visible >= 2, "chunks cover the view");
            Check.Equal(visible, map.LastDraw.ChunkOutlines, "an outline is drawn for each resident chunk in view");
            Check.True(resident.Any(chunk => chunk.Content == ChunkContent.Derived) && resident.Any(chunk => chunk.Content == ChunkContent.Native), "native cells and derived gradient chunks are resident");

            shell.SetResidency(false);
            await runner.Frames(2);
            Check.Equal(0, map.LastDraw.ChunkOutlines, "turned off, nothing is outlined");
            Check.True(!shell.LayerList.Rows.Contains("") && shell.LayerList.Rows.Any(row => row.StartsWith("DEVELOPER TOOLS", StringComparison.Ordinal)), "the layer panel lists the developer family");

            shell.RefreshPanels();
            var statistics = world.Atlas.CacheStatistics;
            var hud = shell.Hud.Text;
            Check.Contains(hud, "active chunk: local chunk (48, 48) (Loaded) of scratch_gradient", "the chunk under the camera centre, at the overlay's level, with its residency");
            var megabytes = (long bytes) => (bytes / 1048576.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            Check.Contains(hud, $"cache: {statistics.ResidentChunks} chunks, {megabytes(statistics.ResidentBytes)} MiB of 64.0 MiB, loads {statistics.Loads}, evictions {statistics.Evictions}", "cache statistics");
            Check.True(statistics.Loads >= statistics.ResidentChunks && statistics.ResidentChunks > 0, "something was loaded");

            // The chunk under the camera changes when the camera moves, and its residency with it.
            map.SetCentre(new WorldPoint(80_000, 20_000));
            shell.RefreshPanels();
            Check.Contains(shell.Hud.Text, "active chunk: local chunk (78, 19)", "the camera centre moved to another chunk");
            await runner.Frames(2);
            map.RefreshOverlay(force: true);
            shell.RefreshPanels();
            Check.Contains(shell.Hud.Text, "active chunk: local chunk (78, 19) (Loaded) of scratch_gradient", "sampling the new view loaded it");

            shell.SetLayer(0);
            shell.RefreshPanels();
            var chunkLine = shell.Hud.Text.Split('\n').Single(line => line.StartsWith("active chunk:", StringComparison.Ordinal));
            Check.True(chunkLine.StartsWith("active chunk: local chunk (78, 19) (", StringComparison.Ordinal) && chunkLine.EndsWith(" of scratch", StringComparison.Ordinal), $"without a field layer the chunk is the local one of the developer scratch field: {chunkLine}");
            shell.Dispatch(InputActions.LevelCoarser);
            shell.SetLayer(shell.Layers.ToList().FindIndex(layer => layer.Key == "scratch"));
            shell.RefreshPanels();
            Check.Contains(shell.Hud.Text, "active chunk: regional chunk (", "a fixed regional level makes the active chunk regional");
            shell.Dispatch(InputActions.LevelFiner);
            shell.Dispatch(InputActions.LevelFiner);
            shell.RefreshPanels();
            Check.Equal(GridLevel.Tactical, shell.ViewLevel, "two steps finer from regional is tactical");
            Check.Contains(shell.Hud.Text, "active chunk: local chunk (78, 19)", "finer than native, the active chunk is the native chunk that answers the view");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task ToolsAreJournaled()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--layer", "scratch", "--center", "50000,50000", "--zoom", "2", "--no-hud");
        var scenario = main.Launch!.Selection!.Scenario;
        var world = main.Host!.World;
        try
        {
            var shell = main.Shell!;
            var map = shell.Map;
            Vector2 cursorAt(float dx, float dy) => shell.Map.GlobalPosition + (shell.Map.Size / 2) + new Vector2(dx, dy);
            InputEventMouseMotion At(float dx, float dy) => new() { Position = cursorAt(dx, dy), GlobalPosition = cursorAt(dx, dy) };
            WorldPoint WorldOf(float dx, float dy) => map.Transform.ToWorld(cursorAt(dx, dy) - map.GlobalPosition);
            Check.Contains(shell.Status.ToolsText, "paint: radius 500 m, value 1.00   |   survey line: none pending", "the status line shows the default brush");
            Check.Contains(shell.Status.HelpText, "Paint: P", "the help line lists the paint key");

            shell.Dispatch(InputActions.PaintScratch, At(40, -30));
            var paint = world.Journal.Entries.Single();
            Check.True(paint.Command is PaintScratch stroke && stroke.RadiusM == 500 && stroke.Value == 1.0 && stroke.Centre == WorldOf(40, -30) && paint.Rejection is null, "P journals PaintScratch at the cursor with the brush");
            Check.True(map.Overlay is not null, "the overlay exists");
            map.RefreshOverlay();
            Check.Near(1.0, map.Overlay!.DataMax, 1e-9, "the new paint shows in the overlay straight away, without waiting for the refresh interval");
            Check.Contains(shell.Status.ToolsText, "radius 500 m", "state unchanged by painting");

            shell.Dispatch(InputActions.PaintRadiusUp);
            Check.Contains(shell.Status.ToolsText, "radius 1 km", "radius up");
            shell.Dispatch(InputActions.PaintRadiusDown);
            shell.Dispatch(InputActions.PaintRadiusDown);
            shell.Dispatch(InputActions.PaintRadiusDown);
            Check.Contains(shell.Status.ToolsText, "radius 100 m", "radius down three steps");
            for (var i = 0; i < 3; i++)
            {
                shell.Dispatch(InputActions.PaintValueDown);
            }

            Check.Contains(shell.Status.ToolsText, "value 0.70", "value down three steps");
            shell.Dispatch(InputActions.PaintScratch, At(-60, 20));
            Check.True(world.Journal.Entries[1].Command is PaintScratch second && second.RadiusM == 100 && Math.Abs(second.Value - 0.7) < 1e-12, "the second stroke uses the new radius and value");
            shell.Dispatch(InputActions.PaintValueUp);
            Check.Contains(shell.Status.ToolsText, "value 0.80", "value up");

            // Keys reach the same actions: Shift+= is the radius, not the zoom.
            var zoom = map.Transform.PixelsPerMetre;
            Press(Key.Equal, shift: true);
            Check.Contains(shell.Status.ToolsText, "radius 200 m", "Shift+= raises the radius");
            Check.Equal(zoom, map.Transform.PixelsPerMetre, "and does not zoom");
            Press(Key.Minus, shift: true);
            Press(Key.Bracketleft, shift: true);
            Check.Contains(shell.Status.ToolsText, "radius 100 m, value 0.70", "Shift+- and Shift+[ lower radius and value");
            Press(Key.Bracketright, shift: true);
            Check.Contains(shell.Status.ToolsText, "value 0.80", "Shift+] raises the value");
            Check.Equal(2, world.Journal.Entries.Count, "brush changes are host state and are not journaled");

            // Painting and adding survey points use the pointer, which a headless run does not have: with no pointer over the map the tools refuse.
            Press(Key.P);
            Check.Equal(2, world.Journal.Entries.Count, "P with the pointer away from the map paints nothing");
            shell.Dispatch(InputActions.PaintScratch, At(100, 100));
            Check.Equal(3, world.Journal.Entries.Count, "painting at a pointer over the map journals a stroke");
            Check.True(world.Journal.Entries[2].Command is PaintScratch third && third.Centre == WorldOf(100, 100) && third.RadiusM == 100, "at the pointer");

            // Survey line: add points, finish.
            Check.True(shell.Tools.Pending.Count == 0, "no pending line");
            shell.Dispatch(InputActions.SurveyAddPoint, At(-200, 150));
            shell.Dispatch(InputActions.SurveyFinish);
            Check.Equal(3, world.Journal.Entries.Count, "a one-point line is not submitted");
            shell.Dispatch(InputActions.SurveyAddPoint, At(-100, 150));
            shell.Dispatch(InputActions.SurveyAddPoint, At(50, 80));
            shell.Dispatch(InputActions.SurveyAddPoint, At(50, 80));
            Check.Equal(3, shell.Tools.Pending.Count, "a repeated point is ignored");
            Check.Equal(3, map.PendingLine.Count, "the map draws the pending line");
            Check.Contains(shell.Status.ToolsText, "survey line: 3 pending point(s), next label S1", "the status line counts the pending points");
            await runner.Frames(2);
            Check.Equal(3, map.PendingLine.Count, "the pending line is still there after a redraw with it");
            shell.Dispatch(InputActions.SurveyCancel);
            Check.Equal(0, map.PendingLine.Count, "J drops the pending line");
            Check.Contains(shell.Status.ToolsText, "survey line: none pending", "and the status line says so");
            Check.Equal(3, world.Journal.Entries.Count, "a cancelled line is never journaled");

            shell.Dispatch(InputActions.SurveyAddPoint, At(-200, 150));
            shell.Dispatch(InputActions.SurveyAddPoint, At(-100, 150));
            shell.Dispatch(InputActions.SurveyAddPoint, At(50, 80));
            Press(Key.K);
            var drawn = world.Journal.Entries[3];
            Check.True(drawn.Command is DrawSurveyLine line && line.Label == "S1" && line.Points.Count == 3 && line.Points[0] == WorldOf(-200, 150) && drawn.Rejection is null, "K journals DrawSurveyLine S1 with the pending points");
            Check.Equal(1, world.Features.Count, "the world has the line");
            Check.Equal(0, shell.Tools.Pending.Count, "the pending line is cleared once accepted");

            shell.Dispatch(InputActions.SurveyAddPoint, At(-200, -150));
            shell.Dispatch(InputActions.SurveyAddPoint, At(150, -150));
            shell.Dispatch(InputActions.SurveyFinish);
            Check.True(world.Journal.Entries[4].Command is DrawSurveyLine { Label: "S2" }, "the next line is S2");
            await runner.Frames(2);
            Check.Equal(2, map.LastDraw.SurveyLines, "both lines are drawn");
            shell.SetSurveyLines(false);
            await runner.Frames(2);
            Check.Equal(0, map.LastDraw.SurveyLines, "V hides survey lines");
            shell.Dispatch(InputActions.ToggleSurveyLines);
            Check.True(shell.SurveyLinesVisible, "and shows them again");

            // Removal: the nearest survey line to the selection.
            shell.SelectLocation(WorldOf(0, 1_000));
            shell.Dispatch(InputActions.SurveyRemove);
            Check.Equal(5, world.Journal.Entries.Count, "nothing near the selection: nothing removed");
            shell.SelectLocation(WorldOf(-20, -150));
            await runner.Frames(1);
            Check.Equal("survey:2", map.HighlightedFeature, "the line nearest the selection is highlighted");
            Press(Key.X);
            Check.True(world.Journal.Entries[5].Command is RemoveSurveyLine remove && remove.Line == new EntityId<Kind.SurveyLine>(2) && world.Journal.Entries[5].Rejection is null, "X journals RemoveSurveyLine of the nearest line");
            Check.Equal(1, world.Features.Count, "one line left");
            Check.Equal("survey:1", world.Features[0].Id, "the other one");

            Check.Equal(0, main.Bindings.FindConflicts().Count, "the new bindings conflict with nothing");
            var actions = main.Inputs.Snapshot().Select(handled => handled.Action).ToList();
            foreach (var action in new[] { InputActions.PaintScratch, InputActions.PaintRadiusUp, InputActions.PaintValueDown, InputActions.SurveyAddPoint, InputActions.SurveyFinish, InputActions.SurveyCancel, InputActions.SurveyRemove, InputActions.ToggleSurveyLines })
            {
                Check.True(actions.Contains(action), $"input history lists {action}");
            }

            var live = world.ComputeChecksum();
            var replayed = Replay.Run(scenario, world.Seed, world.Journal.Entries, world.Now);
            Check.Equal(live, replayed.ComputeChecksum(), "replaying the tool commands reproduces the live state");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task InspectorShowsInputs()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--layer", "scratch", "--center", "50000,50000", "--zoom", "2", "--no-hud",
            "--paint", "50000,50000,300,0.6", "--line", "49900,50040;50100,50040");
        try
        {
            var shell = main.Shell!;
            shell.SelectLocation(new WorldPoint(50_020, 50_015));
            var text = shell.Inspector.Lines.Select(line => line.Text).ToList();
            Check.Contains(text[2], "Inspection level: local (16 m) (from the layer's zoom;", "the level note");
            var reading = text.IndexOf("  developer scratch: 0.6 (dimensionless)");
            Check.True(reading >= 0, $"the painted value is shown with its unit:\n{string.Join('\n', text)}");
            Check.Equal("      level: local (16 m)", text[reading + 1], "level");
            Check.Contains(text[reading + 2], "classification: stored: bilinear over native cells", "classification");
            Check.True(text.Skip(reading + 8).Take(4).All(line => line.StartsWith("        cell local[", StringComparison.Ordinal) && line.Contains(" w=", StringComparison.Ordinal) && line.Contains(" v=0.600 (stored)", StringComparison.Ordinal)), "four stored local cells with weights");
            Check.True(text.Contains("Nearby features within 28 m"), "the features section is present, with the 14 px pick radius in metres");
            Check.True(text.Any(line => line.StartsWith("  survey:1 \"S1\": survey line, 25 m away", StringComparison.Ordinal)), "the survey line 25 m north of the point is listed with its distance");
            Check.Equal("survey:1", shell.Map.HighlightedFeature, "and highlighted on the map");

            shell.SelectLocation(new WorldPoint(10_000, 10_000));
            Check.True(shell.Map.HighlightedFeature is null, "nothing is highlighted far from every line");
            Check.True(shell.Inspector.Lines.Any(line => line.Text.StartsWith("Nearby features within ", StringComparison.Ordinal) && line.Text.EndsWith(": none", StringComparison.Ordinal)), "no nearby features");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task LaunchOptionsReproduceView()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--paint", "30000,40000,1500,0.8;31000,41000,600,0.3", "--line", "20000,20000;50000,60000;90000,55000", "--residency",
            "--level", "regional", "--layer", "Developer scratch");
        try
        {
            var shell = main.Shell!;
            var world = main.Host!.World;
            Check.Equal(3, world.Journal.Entries.Count, "two strokes and one line were submitted at startup");
            Check.True(world.Journal.Entries[2].Command is DrawSurveyLine { Label: "S1" }, "the line is S1");
            Check.True(shell.ResidencyVisible && shell.SurveyLinesVisible, "--residency turns the overlay on; survey lines default to on");
            Check.Equal(GridLevel.Regional, shell.ViewLevel, "--level fixes the level");
            Check.Equal("scratch", shell.ActiveLayer.Key, "--layer takes a field name");
            Check.Equal(world.Identity.Start, world.Now, "startup commands apply at the start time");
            Check.Contains(shell.Status.ToolsText, "view level: regional (256 m), fixed", "the status line shows it");
            Check.True(shell.Map.Overlay is { Level: GridLevel.Regional }, "the overlay is sampled at the fixed level");
            await runner.Frames(2);
            Check.True(shell.Map.LastDraw is { OverlayTexture: true, SurveyLines: 1 } && shell.Map.LastDraw.ChunkOutlines > 0, "overlay, survey line and chunk outlines are all drawn");
        }
        finally
        {
            await Dispose(main);
        }
    }
}

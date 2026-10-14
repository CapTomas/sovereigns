using Sovereigns.Client.View;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Client.Tests;

/// <summary>Coordinate convention and view behaviour (ADR-0004 §2).</summary>
public static class ViewTests
{
    private static readonly WorldExtent Extent = new(100_000, 100_000);

    public static IEnumerable<TestCase> Cases() =>
    [
        new("view: north is up and east is right (y flipped at the client boundary)", NorthIsUp),
        new("view: ToWorld(ToScreen(p)) round-trips within 1e-6 m across the 100 km extent", RoundTrips),
        new("view: float32 screen error does not grow with distance from the world origin", FloatErrorIsPositionIndependent),
        new("view: zooming at the cursor keeps the world point under the cursor fixed, and zoom is clamped", ZoomKeepsPointUnderCursor),
        new("view: fit shows the whole world; scale and grid choices follow the zoom", FitAndScales),
    ];

    private static Task NorthIsUp()
    {
        var view = new ViewTransform(50_000, 50_000, 0.01, new Vector2(800, 600));
        var centre = view.ToScreen(new WorldPoint(50_000, 50_000));
        Check.Equal(new Vector2(400, 300), centre, "world centre maps to the viewport centre");
        Check.True(view.ToScreen(new WorldPoint(50_000, 60_000)).Y < view.ToScreen(new WorldPoint(50_000, 40_000)).Y, "north is above south on screen");
        Check.True(view.ToScreen(new WorldPoint(60_000, 50_000)).X > view.ToScreen(new WorldPoint(40_000, 50_000)).X, "east is right of west on screen");
        Check.Near(10_000, view.ToWorld(new Vector2(500, 300)).X - view.ToWorld(new Vector2(400, 300)).X, 1e-9, "100 px span 10,000 m at 0.01 px/m");
        return Task.CompletedTask;
    }

    private static Task RoundTrips()
    {
        double[] zooms = [ViewTransform.MinPixelsPerMetre, 0.01, 1, ViewTransform.MaxPixelsPerMetre];
        (double X, double Y)[] centres = [(1, 1), (50_000, 50_000), (99_999.123456, 99_998.5), (0, 100_000)];
        var worst = 0.0;
        foreach (var zoom in zooms)
        {
            foreach (var (cx, cy) in centres)
            {
                var view = new ViewTransform(cx, cy, zoom, new Vector2(1600, 900));
                for (var i = 0; i <= 40; i++)
                {
                    var point = new WorldPoint(i * 2_500.001 + 0.123456, 100_000 - i * 2_499.999 - 0.654321);
                    var (sx, sy) = view.ToScreenPixels(point);
                    var back = view.ToWorld(sx, sy);
                    worst = Math.Max(worst, Math.Max(Math.Abs(back.X - point.X), Math.Abs(back.Y - point.Y)));
                }
            }
        }

        Check.True(worst <= 1e-6, $"worst round-trip error {worst} m exceeds 1e-6 m");
        return Task.CompletedTask;
    }

    private static Task FloatErrorIsPositionIndependent()
    {
        // The view centre stays near the point, so float32 is only used for small pixel offsets.
        var errors = new List<double>();
        foreach (var origin in new[] { 1.0, 50_000.0, 99_999.0 })
        {
            var view = new ViewTransform(origin + 3.7, origin + 1.3, 2, new Vector2(1600, 900));
            var point = new WorldPoint(origin + 0.123456789, origin + 0.987654321);
            var back = view.ToWorld(view.ToScreen(point));
            errors.Add(Math.Max(Math.Abs(back.X - point.X), Math.Abs(back.Y - point.Y)));
        }

        foreach (var error in errors)
        {
            Check.True(error <= 0.001 / 2, $"float32 round trip error {error} m is more than a thousandth of a pixel");
        }

        return Task.CompletedTask;
    }

    private static Task ZoomKeepsPointUnderCursor()
    {
        var view = new ViewTransform(30_000, 70_000, 0.01, new Vector2(900, 700));
        var cursor = new Vector2(200, 150);
        var under = view.ToWorld(cursor);
        foreach (var factor in new[] { 1.25, 1.25, 1 / 1.25, 8, 1 / 64.0, 1e6, 1e-9 })
        {
            view = view.ZoomedAt(cursor, factor);
            Check.True(view.PixelsPerMetre is >= ViewTransform.MinPixelsPerMetre and <= ViewTransform.MaxPixelsPerMetre, $"zoom {view.PixelsPerMetre} px/m is outside the clamp");
            var (sx, sy) = view.ToScreenPixels(under);
            Check.Near(cursor.X, sx, 1e-6, $"x of the anchored point after zoom factor {factor}");
            Check.Near(cursor.Y, sy, 1e-6, $"y of the anchored point after zoom factor {factor}");
        }

        return Task.CompletedTask;
    }

    private static Task FitAndScales()
    {
        var fitted = new ViewTransform(0, 0, 1, new Vector2(800, 600)).Fitted(Extent);
        foreach (var corner in new[] { new WorldPoint(0, 0), new WorldPoint(100_000, 100_000), new WorldPoint(0, 100_000), new WorldPoint(100_000, 0) })
        {
            Check.True(new Rect2(0, 0, 800, 600).HasPoint(fitted.ToScreen(corner)), $"corner {corner} is inside the fitted view");
        }

        Check.Equal(10_000.0, MapScale.ChooseGridSpacing(0.01), "grid spacing when zoomed out");
        Check.Equal(1000.0, MapScale.ChooseGridSpacing(0.1), "grid spacing at 0.1 px/m");
        Check.Equal(100.0, MapScale.ChooseGridSpacing(1), "grid spacing at 1 px/m");
        Check.Equal(10.0, MapScale.ChooseGridSpacing(20), "grid spacing when zoomed in");
        Check.Equal(20_000.0, MapScale.ChooseScaleBarLength(0.0075, 160), "scale bar length (1-2-5 steps)");
        Check.Equal("12.5 km", MapScale.FormatDistance(12_500), "kilometre display");
        Check.Equal("250 m", MapScale.FormatDistance(250), "metre display");
        return Task.CompletedTask;
    }
}

using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Client.View;

/// <summary>
/// The 2D orthographic map view as a pure transform between the world frame (metres, +x east, +y north, ADR-0004 §2)
/// and screen pixels (+y down). The centre is held in double precision; world positions are made relative to it
/// in double before anything is narrowed to float32, so precision does not degrade with distance from the origin.
/// Screen coordinates never flow back into simulation state.
/// </summary>
public readonly record struct ViewTransform(double CentreX, double CentreY, double PixelsPerMetre, Vector2 ViewportSize)
{
    public const double MinPixelsPerMetre = 0.001;
    public const double MaxPixelsPerMetre = 20;

    public double MetresPerPixel => 1.0 / PixelsPerMetre;

    public WorldPoint Centre => new(CentreX, CentreY);

    /// <summary>Pixel position in double precision, relative to the viewport's top-left corner.</summary>
    public (double X, double Y) ToScreenPixels(WorldPoint point) =>
        ((point.X - CentreX) * PixelsPerMetre + ViewportSize.X * 0.5,
         ViewportSize.Y * 0.5 - (point.Y - CentreY) * PixelsPerMetre);

    public Vector2 ToScreen(WorldPoint point)
    {
        var (x, y) = ToScreenPixels(point);
        return new Vector2((float)x, (float)y);
    }

    public WorldPoint ToWorld(double screenX, double screenY) =>
        new(CentreX + (screenX - ViewportSize.X * 0.5) / PixelsPerMetre,
            CentreY - (screenY - ViewportSize.Y * 0.5) / PixelsPerMetre);

    public WorldPoint ToWorld(Vector2 screen) => ToWorld(screen.X, screen.Y);

    /// <summary>World rectangle currently inside the viewport.</summary>
    public (double MinX, double MinY, double MaxX, double MaxY) VisibleBounds()
    {
        var southWest = ToWorld(0, ViewportSize.Y);
        var northEast = ToWorld(ViewportSize.X, 0);
        return (southWest.X, southWest.Y, northEast.X, northEast.Y);
    }

    public ViewTransform WithViewport(Vector2 size) => this with { ViewportSize = size };

    /// <summary>Moves the view as if the map were dragged by <paramref name="pixels"/> on screen.</summary>
    public ViewTransform Dragged(Vector2 pixels) =>
        this with { CentreX = CentreX - pixels.X / PixelsPerMetre, CentreY = CentreY + pixels.Y / PixelsPerMetre };

    /// <summary>Moves the view toward a screen direction (up is north), by a distance given in pixels.</summary>
    public ViewTransform Panned(Vector2 direction, double pixels) =>
        this with { CentreX = CentreX + direction.X * pixels / PixelsPerMetre, CentreY = CentreY - direction.Y * pixels / PixelsPerMetre };

    /// <summary>Scales the view by <paramref name="factor"/> (clamped) keeping the world point under <paramref name="anchor"/> fixed.</summary>
    public ViewTransform ZoomedAt(Vector2 anchor, double factor)
    {
        var scale = Math.Clamp(PixelsPerMetre * factor, MinPixelsPerMetre, MaxPixelsPerMetre);
        var under = ToWorld(anchor);
        return this with
        {
            PixelsPerMetre = scale,
            CentreX = under.X - (anchor.X - ViewportSize.X * 0.5) / scale,
            CentreY = under.Y + (anchor.Y - ViewportSize.Y * 0.5) / scale,
        };
    }

    /// <summary>Centres the world and chooses the zoom that fits all of it, leaving <paramref name="margin"/> (a fraction) free.</summary>
    public ViewTransform Fitted(WorldExtent extent, double margin = 0.1)
    {
        var usable = 1 - 2 * margin;
        var scale = Math.Min(ViewportSize.X * usable / extent.East, ViewportSize.Y * usable / extent.North);
        return this with
        {
            CentreX = extent.East * 0.5,
            CentreY = extent.North * 0.5,
            PixelsPerMetre = Math.Clamp(scale, MinPixelsPerMetre, MaxPixelsPerMetre),
        };
    }
}

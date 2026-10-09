using Sovereigns.Simulation;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Client.View;

/// <summary>
/// Nonfinal debug rendering of the world frame: extent, adaptive grid, markers, the selected location, scale bar
/// and north arrow. It draws only what the simulation reports; an unavailable layer shows an explicit hatched
/// no-data state rather than any value. World content is positioned relative to the double-precision view centre.
/// </summary>
public partial class WorldView : Control
{
    private const int MaxGridLinesPerAxis = 400;
    private const float HatchSpacing = 18;
    private const int LabelSize = 12;

    private static readonly Color VoidColour = new(0.055f, 0.06f, 0.075f);
    private static readonly Color WorldFill = new(0.12f, 0.145f, 0.17f);
    private static readonly Color NoDataTint = new(0.45f, 0.3f, 0.12f, 0.35f);
    private static readonly Color NoDataHatch = new(0.95f, 0.65f, 0.25f, 0.5f);
    private static readonly Color GridMinor = new(0.55f, 0.65f, 0.75f, 0.16f);
    private static readonly Color GridMajor = new(0.55f, 0.65f, 0.75f, 0.32f);
    private static readonly Color Outline = new(0.85f, 0.9f, 0.95f);
    private static readonly Color TextColour = new(0.8f, 0.86f, 0.92f);
    private static readonly Color MarkerColour = new(1f, 0.55f, 0.15f);
    private static readonly Color SelectionColour = new(0.3f, 0.95f, 1f);

    private ViewTransform _transform = new(0, 0, 1, Vector2.One);
    private bool _followsWindow = true;

    public World? World { get; set; }

    public MapLayer? ActiveLayer { get; set; }

    public WorldPoint? Selection { get; set; }

    public ViewTransform Transform => _transform;

    public override void _Ready()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += OnResized;
    }

    public void PanByKeys(Vector2 direction, double seconds)
    {
        _transform = _transform.Panned(direction, 700 * seconds);
        _followsWindow = false;
    }

    public void PanByDrag(Vector2 pixels)
    {
        _transform = _transform.Dragged(pixels);
        _followsWindow = false;
    }

    public void ZoomAt(Vector2 anchor, double factor)
    {
        _transform = _transform.ZoomedAt(anchor, factor);
        _followsWindow = false;
    }

    /// <summary>Fits the whole world in view; the view then keeps fitting it as the window resizes until the player pans or zooms.</summary>
    public void FitWorld()
    {
        _followsWindow = true;
        if (World is not null && Size.X > 1 && Size.Y > 1)
        {
            _transform = _transform.WithViewport(Size).Fitted(World.Extent);
        }
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var size = Size;
        DrawRect(new Rect2(Vector2.Zero, size), VoidColour);
        if (World is null || size.X < 2 || size.Y < 2)
        {
            return;
        }

        var view = _transform;
        var extent = World.Extent;
        var (left, top) = view.ToScreenPixels(new WorldPoint(0, extent.North));
        var (right, bottom) = view.ToScreenPixels(new WorldPoint(extent.East, 0));

        // Clip in double precision so far-off-screen corners never reach float32.
        var clipLeft = Math.Clamp(left, 0, size.X);
        var clipRight = Math.Clamp(right, 0, size.X);
        var clipTop = Math.Clamp(top, 0, size.Y);
        var clipBottom = Math.Clamp(bottom, 0, size.Y);
        if (clipRight > clipLeft && clipBottom > clipTop)
        {
            DrawRect(Area(clipLeft, clipTop, clipRight, clipBottom), WorldFill);
            if (ActiveLayer is { IsAvailable: false })
            {
                DrawNoData(clipLeft, clipTop, clipRight, clipBottom);
            }

            DrawGrid(view, extent, clipLeft, clipTop, clipRight, clipBottom);
        }

        DrawWorldOutline(left, top, right, bottom, clipLeft, clipTop, clipRight, clipBottom, size);
        DrawOriginLabel(view);
        DrawMarkers(view, ActiveLayer?.Key == MapLayer.DeveloperMarkersKey);
        DrawSelection(view);
        DrawScaleBar(view, size);
        DrawNorthArrow(size);
    }

    private void OnResized()
    {
        _transform = _transform.WithViewport(Size);
        if (_followsWindow)
        {
            FitWorld();
        }
    }

    private static Rect2 Area(double left, double top, double right, double bottom) =>
        new((float)left, (float)top, (float)(right - left), (float)(bottom - top));

    private void DrawNoData(double left, double top, double right, double bottom)
    {
        DrawRect(Area(left, top, right, bottom), NoDataTint);
        var height = bottom - top;
        for (var x = left - height; x <= right; x += HatchSpacing)
        {
            // A "/" stroke from (x, bottom) up to (x + height, top), clipped to the area's left and right sides.
            var startX = Math.Max(x, left);
            var endX = Math.Min(x + height, right);
            if (endX <= startX)
            {
                continue;
            }

            DrawLine(new Vector2((float)startX, (float)(bottom - (startX - x))), new Vector2((float)endX, (float)(bottom - (endX - x))), NoDataHatch);
        }
    }

    private void DrawGrid(ViewTransform view, WorldExtent extent, double clipLeft, double clipTop, double clipRight, double clipBottom)
    {
        var spacing = MapScale.ChooseGridSpacing(view.PixelsPerMetre);
        var bounds = view.VisibleBounds();
        var font = ThemeDB.FallbackFont;
        var firstX = (long)Math.Ceiling(Math.Max(0, bounds.MinX) / spacing);
        var lastX = (long)Math.Floor(Math.Min(extent.East, bounds.MaxX) / spacing);
        for (var k = firstX; k <= lastX && k - firstX < MaxGridLinesPerAxis; k++)
        {
            var x = (float)view.ToScreenPixels(new WorldPoint(k * spacing, 0)).X;
            DrawLine(new Vector2(x, (float)clipTop), new Vector2(x, (float)clipBottom), k % 10 == 0 ? GridMajor : GridMinor);
            DrawString(font, new Vector2(x + 3, 14), MapScale.FormatDistance(k * spacing), HorizontalAlignment.Left, -1, LabelSize, TextColour with { A = 0.7f });
        }

        var firstY = (long)Math.Ceiling(Math.Max(0, bounds.MinY) / spacing);
        var lastY = (long)Math.Floor(Math.Min(extent.North, bounds.MaxY) / spacing);
        for (var k = firstY; k <= lastY && k - firstY < MaxGridLinesPerAxis; k++)
        {
            var y = (float)view.ToScreenPixels(new WorldPoint(0, k * spacing)).Y;
            DrawLine(new Vector2((float)clipLeft, y), new Vector2((float)clipRight, y), k % 10 == 0 ? GridMajor : GridMinor);
            DrawString(font, new Vector2(4, y - 3), MapScale.FormatDistance(k * spacing), HorizontalAlignment.Left, -1, LabelSize, TextColour with { A = 0.7f });
        }
    }

    private void DrawWorldOutline(double left, double top, double right, double bottom,
        double clipLeft, double clipTop, double clipRight, double clipBottom, Vector2 size)
    {
        if (clipBottom > clipTop)
        {
            DrawEdge(left, size.X, vertical: true, clipTop, clipBottom);
            DrawEdge(right, size.X, vertical: true, clipTop, clipBottom);
        }

        if (clipRight > clipLeft)
        {
            DrawEdge(top, size.Y, vertical: false, clipLeft, clipRight);
            DrawEdge(bottom, size.Y, vertical: false, clipLeft, clipRight);
        }
    }

    private void DrawEdge(double position, float limit, bool vertical, double from, double to)
    {
        if (position < 0 || position > limit)
        {
            return;
        }

        var a = vertical ? new Vector2((float)position, (float)from) : new Vector2((float)from, (float)position);
        var b = vertical ? new Vector2((float)position, (float)to) : new Vector2((float)to, (float)position);
        DrawLine(a, b, Outline, 2);
    }

    private void DrawOriginLabel(ViewTransform view)
    {
        var origin = view.ToScreen(new WorldPoint(0, 0));
        if (new Rect2(Vector2.Zero, Size).HasPoint(origin))
        {
            DrawString(ThemeDB.FallbackFont, origin + new Vector2(6, -6), "origin (0, 0): south-west corner", HorizontalAlignment.Left, -1, LabelSize, Outline);
        }
    }

    private void DrawMarkers(ViewTransform view, bool labelled)
    {
        var bounds = new Rect2(-20, -20, Size.X + 40, Size.Y + 40);
        foreach (var marker in World!.Markers)
        {
            var at = view.ToScreen(marker.Position);
            if (!bounds.HasPoint(at))
            {
                continue;
            }

            DrawCircle(at, 6, Colors.Black);
            DrawCircle(at, 4.5f, MarkerColour);
            if (labelled)
            {
                DrawString(ThemeDB.FallbackFont, at + new Vector2(9, -7), $"{marker.Label} ({marker.Id})", HorizontalAlignment.Left, -1, LabelSize + 1, MarkerColour);
            }
        }
    }

    private void DrawSelection(ViewTransform view)
    {
        if (Selection is not { } point)
        {
            return;
        }

        var at = view.ToScreen(point);
        if (!new Rect2(-20, -20, Size.X + 40, Size.Y + 40).HasPoint(at))
        {
            return;
        }

        DrawArc(at, 8, 0, Mathf.Tau, 24, SelectionColour, 1.5f);
        DrawLine(at + new Vector2(-14, 0), at + new Vector2(-4, 0), SelectionColour, 1.5f);
        DrawLine(at + new Vector2(4, 0), at + new Vector2(14, 0), SelectionColour, 1.5f);
        DrawLine(at + new Vector2(0, -14), at + new Vector2(0, -4), SelectionColour, 1.5f);
        DrawLine(at + new Vector2(0, 4), at + new Vector2(0, 14), SelectionColour, 1.5f);
    }

    private void DrawScaleBar(ViewTransform view, Vector2 size)
    {
        var length = MapScale.ChooseScaleBarLength(view.PixelsPerMetre, 160);
        var pixels = (float)(length * view.PixelsPerMetre);
        var start = new Vector2(16, size.Y - 28);
        var end = start + new Vector2(pixels, 0);
        DrawLine(start, end, TextColour, 2);
        DrawLine(start + new Vector2(0, -5), start + new Vector2(0, 5), TextColour, 2);
        DrawLine(end + new Vector2(0, -5), end + new Vector2(0, 5), TextColour, 2);
        DrawString(ThemeDB.FallbackFont, start + new Vector2(0, -9), $"{MapScale.FormatDistance(length)} (scale bar)", HorizontalAlignment.Left, -1, LabelSize, TextColour);
    }

    private void DrawNorthArrow(Vector2 size)
    {
        var tail = new Vector2(size.X - 36, 74);
        var tip = new Vector2(size.X - 36, 34);
        DrawLine(tail, tip, TextColour, 2);
        DrawColoredPolygon([tip + new Vector2(0, -2), tip + new Vector2(-6, 10), tip + new Vector2(6, 10)], TextColour);
        DrawString(ThemeDB.FallbackFont, tip + new Vector2(-4, -8), "N", HorizontalAlignment.Left, -1, 14, TextColour);
    }
}

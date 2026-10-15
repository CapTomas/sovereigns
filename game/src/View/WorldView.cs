using Sovereigns.Simulation;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Client.View;

/// <summary>What the last draw put on the map; lets tests and the HUD confirm that overlays were really drawn.</summary>
public readonly record struct DrawStats(bool OverlayTexture, int Arrows, int ChunkOutlines, int SurveyLines);

/// <summary>
/// Nonfinal debug rendering of the world frame: extent, adaptive grid, markers, the selected location, scale bar
/// and north arrow, plus atlas field overlays (greyscale scalars, vector arrows), survey lines and the
/// chunk-residency outlines. It draws only what the simulation reports: overlays are sampled from
/// <see cref="FieldAtlas"/> queries, and an unavailable layer shows an explicit hatched no-data state rather than any
/// value. World content is positioned relative to the double-precision view centre.
/// </summary>
public partial class WorldView : Control
{
    /// <summary>Longest wait between overlay rebuilds while the camera or clock keeps changing; commands rebuild at once.</summary>
    public const double OverlayRefreshSeconds = 0.25;

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
    private static readonly Color VoidHatch = new(0.95f, 0.65f, 0.25f, 0.22f);
    private static readonly Color SurveyColour = new(1f, 0.86f, 0.2f);
    private static readonly Color ArrowColour = new(0.96f, 0.97f, 1f);

    private readonly record struct OverlayKey(string Field, GridLevel Level, ViewTransform View, long Time, int Commands);

    private ViewTransform _transform = new(0, 0, 1, Vector2.One);
    private bool _followsWindow = true;
    private FieldOverlay? _overlay;
    private IReadOnlyList<OverlayArrow> _arrows = [];
    private ImageTexture? _texture;
    private OverlayKey? _overlayKey;
    private double _sinceOverlay = double.MaxValue;
    private bool _overlayDirty = true;

    public World? World { get; set; }

    public MapLayer? ActiveLayer { get; set; }

    public WorldPoint? Selection { get; set; }

    public ViewTransform Transform => _transform;

    /// <summary>A level chosen with the level keys; null lets the zoom choose (<see cref="ViewLevels.ForZoom"/>).</summary>
    public GridLevel? LevelOverride { get; set; }

    /// <summary>The atlas level overlays are sampled at now.</summary>
    public GridLevel EffectiveLevel => LevelOverride ?? ViewLevels.ForZoom(_transform.PixelsPerMetre);

    /// <summary>The field overlay as last sampled; null for layers without one or while nothing of the world is in view.</summary>
    public FieldOverlay? Overlay => _overlay;

    public bool ShowSurveyLines { get; set; } = true;

    public bool ShowResidency { get; set; }

    /// <summary>Points of the survey line being drawn, shown dashed.</summary>
    public IReadOnlyList<WorldPoint> PendingLine { get; set; } = [];

    /// <summary>ID of the feature to emphasise, normally the one nearest the selection.</summary>
    public string? HighlightedFeature { get; set; }

    public DrawStats LastDraw { get; private set; }

    /// <summary>Raised after the overlay was sampled again (or cleared), so the legend can follow.</summary>
    public event Action? OverlayRebuilt;

    /// <summary>Samples the overlay again at the next opportunity, regardless of the refresh interval.</summary>
    public void InvalidateOverlay() => _overlayDirty = true;

    /// <summary>Puts the camera at a place and scale; the view then stops fitting itself to the window.</summary>
    public void SetView(WorldPoint centre, double metresPerPixel)
    {
        _transform = _transform with
        {
            CentreX = centre.X,
            CentreY = centre.Y,
            PixelsPerMetre = Math.Clamp(1 / metresPerPixel, ViewTransform.MinPixelsPerMetre, ViewTransform.MaxPixelsPerMetre),
        };
        _followsWindow = false;
    }

    /// <summary>Moves the camera centre, keeping the scale.</summary>
    public void SetCentre(WorldPoint centre)
    {
        _transform = _transform with { CentreX = centre.X, CentreY = centre.Y };
        _followsWindow = false;
    }

    public override void _Ready()
    {
        // Each overlay pixel is one atlas sample: nearest filtering keeps the GPU from blending samples into values the
        // atlas never answered, and keeps the no-data edge crisp.
        TextureFilter = TextureFilterEnum.Nearest;
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

    public override void _Process(double delta)
    {
        _sinceOverlay += delta;
        RefreshOverlay();
        QueueRedraw();
    }

    /// <summary>
    /// Samples the active field layer over the visible part of the world when something changed: the camera, the
    /// level, the layer, a command, or the clock for a field that varies with time. Camera and clock changes wait for
    /// <see cref="OverlayRefreshSeconds"/>; a command (<see cref="InvalidateOverlay"/>) or <paramref name="force"/> does not.
    /// </summary>
    public void RefreshOverlay(bool force = false)
    {
        var field = ActiveLayer is { IsFieldOverlay: true } layer ? layer.Field : null;
        if (field is null || World is null || Size.X < 2 || Size.Y < 2)
        {
            if (_overlay is not null)
            {
                ClearOverlay();
            }

            _overlayKey = null;
            return;
        }

        var key = new OverlayKey(field.Key, EffectiveLevel, _transform, VariesWithTime(field) ? World.Now.Milliseconds : 0, World.Journal.Entries.Count);
        if (!(force || _overlayDirty || _overlayKey is null || (key != _overlayKey && _sinceOverlay >= OverlayRefreshSeconds)))
        {
            return;
        }

        _overlayKey = key;
        _overlayDirty = false;
        _sinceOverlay = 0;
        var bounds = _transform.VisibleBounds();
        var extent = World.Extent;
        var area = (MinX: Math.Max(0, bounds.MinX), MinY: Math.Max(0, bounds.MinY), MaxX: Math.Min(extent.East, bounds.MaxX), MaxY: Math.Min(extent.North, bounds.MaxY));
        if (!(area.MaxX > area.MinX && area.MaxY > area.MinY))
        {
            ClearOverlay();
            return;
        }

        var spacing = (field.Kind == FieldKind.Vector ? ViewLevels.ArrowPixels : ViewLevels.SamplePixels) * _transform.MetresPerPixel;
        _overlay = FieldOverlay.Sample(World.Atlas, field, key.Level, area, spacing);
        _arrows = _overlay.Arrows();
        _texture = _overlay.IsVector ? null : ImageTexture.CreateFromImage(_overlay.ToImage());
        OverlayRebuilt?.Invoke();
    }

    /// <summary>Dynamic fields evolve, and local solar time is a function of the clock; everything else changes only by command.</summary>
    private static bool VariesWithTime(FieldDefinition field) =>
        field.Class == FieldClass.Dynamic || field.Key == FieldCatalog.LocalSolarTime.Key;

    private void ClearOverlay()
    {
        _overlay = null;
        _texture = null;
        _arrows = [];
        OverlayRebuilt?.Invoke();
    }

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
        var showsField = ActiveLayer is { IsFieldOverlay: true };
        if (showsField)
        {
            // Outside the world there is no data; a field overlay says so by hatching the void.
            DrawHatch(0, 0, size.X, size.Y, VoidHatch);
        }

        var (left, top) = view.ToScreenPixels(new WorldPoint(0, extent.North));
        var (right, bottom) = view.ToScreenPixels(new WorldPoint(extent.East, 0));

        // Clip in double precision so far-off-screen corners never reach float32.
        var clipLeft = Math.Clamp(left, 0, size.X);
        var clipRight = Math.Clamp(right, 0, size.X);
        var clipTop = Math.Clamp(top, 0, size.Y);
        var clipBottom = Math.Clamp(bottom, 0, size.Y);
        var texture = false;
        var arrows = 0;
        if (clipRight > clipLeft && clipBottom > clipTop)
        {
            DrawRect(Area(clipLeft, clipTop, clipRight, clipBottom), WorldFill);
            if (ActiveLayer is { IsAvailable: false })
            {
                DrawNoData(clipLeft, clipTop, clipRight, clipBottom);
            }
            else if (_overlay is not null)
            {
                texture = DrawScalarOverlay(view);
                arrows = DrawArrows(view);
            }

            DrawGrid(view, extent, clipLeft, clipTop, clipRight, clipBottom);
        }

        DrawWorldOutline(left, top, right, bottom, clipLeft, clipTop, clipRight, clipBottom, size);
        DrawOriginLabel(view);
        DrawMarkers(view, ActiveLayer?.Key == MapLayer.DeveloperMarkersKey);
        var lines = ShowSurveyLines ? DrawSurveyLines(view) : 0;
        DrawPendingLine(view);
        var chunks = ShowResidency ? DrawResidency(view) : 0;
        DrawSelection(view);
        DrawScaleBar(view, size);
        DrawNorthArrow(size);
        if (ShowResidency)
        {
            DrawResidencyKey(size);
        }

        LastDraw = new DrawStats(texture, arrows, chunks, lines);
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
        DrawHatch(left, top, right, bottom, NoDataHatch);
    }

    private void DrawHatch(double left, double top, double right, double bottom, Color colour)
    {
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

            DrawLine(new Vector2((float)startX, (float)(bottom - (startX - x))), new Vector2((float)endX, (float)(bottom - (endX - x))), colour);
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

    /// <summary>Draws the sampled greyscale picture over the part of the world it covers; false when this layer has none.</summary>
    private bool DrawScalarOverlay(ViewTransform view)
    {
        if (_overlay is not { IsVector: false } overlay || _texture is null)
        {
            return false;
        }

        var (left, top) = view.ToScreenPixels(new WorldPoint(overlay.West, overlay.North));
        var (right, bottom) = view.ToScreenPixels(new WorldPoint(overlay.East, overlay.South));
        DrawTextureRect(_texture, Area(left, top, right, bottom), false);
        return true;
    }

    /// <summary>Draws one arrow per sampled vector, its length proportional to the magnitude; returns how many were drawn.</summary>
    private int DrawArrows(ViewTransform view)
    {
        if (_overlay is not { IsVector: true } overlay || overlay.ArrowReference <= 0)
        {
            return 0;
        }

        var maxLength = overlay.SpacingM * view.PixelsPerMetre * 0.9;
        var bounds = new Rect2(-40, -40, Size.X + 80, Size.Y + 80);
        var drawn = 0;
        foreach (var arrow in _arrows)
        {
            var tail = view.ToScreen(arrow.Origin);
            if (!bounds.HasPoint(tail))
            {
                continue;
            }

            var length = (float)(maxLength * arrow.Magnitude / overlay.ArrowReference);
            var direction = new Vector2((float)(arrow.East / arrow.Magnitude), (float)(-arrow.North / arrow.Magnitude));
            var tip = tail + (direction * length);
            drawn++;
            DrawCircle(tail, 2.5f, Colors.Black);
            if (length < 3)
            {
                DrawCircle(tail, 1.5f, ArrowColour);
                continue;
            }

            var head = Math.Min(7f, length * 0.4f);
            var side = new Vector2(-direction.Y, direction.X) * head * 0.55f;
            Vector2[] triangle = [tip, tip - (direction * head) + side, tip - (direction * head) - side];
            DrawLine(tail, tip - (direction * head * 0.5f), Colors.Black, 4);
            DrawColoredPolygon([triangle[0] + (direction * 1.5f), triangle[1] - (direction * 0.5f) + (side * 0.3f), triangle[2] - (direction * 0.5f) - (side * 0.3f)], Colors.Black);
            DrawLine(tail, tip - (direction * head * 0.5f), ArrowColour, 2);
            DrawColoredPolygon(triangle, ArrowColour);
            DrawCircle(tail, 1.5f, ArrowColour);
        }

        return drawn;
    }

    /// <summary>Draws every feature of the world as a labelled polyline, emphasising the highlighted one; returns how many were drawn.</summary>
    private int DrawSurveyLines(ViewTransform view)
    {
        var font = ThemeDB.FallbackFont;
        var count = 0;
        foreach (var feature in World!.Features)
        {
            var points = feature.Points.Select(view.ToScreen).ToArray();
            if (points.Length < 2)
            {
                continue;
            }

            count++;
            var highlighted = feature.Id == HighlightedFeature;
            if (highlighted)
            {
                DrawPolyline(points, SelectionColour with { A = 0.55f }, 10);
            }

            DrawPolyline(points, Colors.Black with { A = 0.85f }, 5);
            DrawPolyline(points, SurveyColour, 2.5f);
            foreach (var point in points)
            {
                DrawCircle(point, 4, Colors.Black);
                DrawCircle(point, 2.8f, SurveyColour);
            }

            var label = $"{feature.Label} ({feature.Id}){(highlighted ? "  < nearest to selection" : "")}";
            DrawStringOutline(font, points[0] + new Vector2(8, -8), label, HorizontalAlignment.Left, -1, LabelSize + 1, 4, Colors.Black);
            DrawString(font, points[0] + new Vector2(8, -8), label, HorizontalAlignment.Left, -1, LabelSize + 1, highlighted ? Colors.White : SurveyColour);
        }

        return count;
    }

    /// <summary>The survey line being drawn: dashed segments between its points, each point numbered.</summary>
    private void DrawPendingLine(ViewTransform view)
    {
        if (PendingLine.Count == 0)
        {
            return;
        }

        var font = ThemeDB.FallbackFont;
        var points = PendingLine.Select(view.ToScreen).ToArray();
        for (var i = 1; i < points.Length; i++)
        {
            DrawDashedLine(points[i - 1], points[i], Colors.Black, 5, 9);
            DrawDashedLine(points[i - 1], points[i], SurveyColour, 2.5f, 9);
        }

        for (var i = 0; i < points.Length; i++)
        {
            DrawArc(points[i], 6, 0, Mathf.Tau, 16, Colors.Black, 4.5f);
            DrawArc(points[i], 6, 0, Mathf.Tau, 16, SurveyColour, 2);
            DrawStringOutline(font, points[i] + new Vector2(9, -8), (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment.Left, -1, LabelSize + 1, 4, Colors.Black);
            DrawString(font, points[i] + new Vector2(9, -8), (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment.Left, -1, LabelSize + 1, SurveyColour);
        }
    }

    /// <summary>The chunks of every field the atlas cache holds, outlined and coloured by content; returns how many were drawn.</summary>
    private int DrawResidency(ViewTransform view)
    {
        var font = ThemeDB.FallbackFont;
        var drawn = 0;
        foreach (var outline in ChunkOutlines.From(World!.Atlas.ResidentChunks()))
        {
            var (west, south, east, north) = outline.Key.Bounds;
            var (left, top) = view.ToScreenPixels(new WorldPoint(west, north));
            var (right, bottom) = view.ToScreenPixels(new WorldPoint(east, south));
            if (right < 0 || bottom < 0 || left > Size.X || top > Size.Y)
            {
                continue;
            }

            drawn++;

            // Chunks of one key with different content share an edge; nesting them keeps each visible.
            var inset = (int)outline.Content * 3;
            var colour = ChunkOutlines.ColourOf(outline.Content);
            (left, top, right, bottom) = (left + inset, top + inset, right - inset, bottom - inset);
            DrawClippedOutline(left, top, right, bottom, colour, 1 + ((int)outline.Key.Level * 0.5f));
            if (right - left >= 150 && bottom - top >= 40)
            {
                var label = $"{ChunkOutlines.NameOf(outline.Content)} {outline.Key}";
                var at = new Vector2((float)Math.Max(left, 0) + 6, (float)Math.Max(top, 0) + 14 + inset);
                DrawStringOutline(font, at, label, HorizontalAlignment.Left, -1, LabelSize, 4, Colors.Black);
                DrawString(font, at, label, HorizontalAlignment.Left, -1, LabelSize, colour);
            }
        }

        return drawn;
    }

    /// <summary>Outlines a rectangle given in double-precision pixels, drawing only the parts of its sides inside the view.</summary>
    private void DrawClippedOutline(double left, double top, double right, double bottom, Color colour, float width)
    {
        var (clipTop, clipBottom) = (Math.Clamp(top, 0, Size.Y), Math.Clamp(bottom, 0, Size.Y));
        var (clipLeft, clipRight) = (Math.Clamp(left, 0, Size.X), Math.Clamp(right, 0, Size.X));
        foreach (var x in new[] { left, right })
        {
            if (x >= 0 && x <= Size.X && clipBottom > clipTop)
            {
                DrawLine(new Vector2((float)x, (float)clipTop), new Vector2((float)x, (float)clipBottom), colour, width);
            }
        }

        foreach (var y in new[] { top, bottom })
        {
            if (y >= 0 && y <= Size.Y && clipRight > clipLeft)
            {
                DrawLine(new Vector2((float)clipLeft, (float)y), new Vector2((float)clipRight, (float)y), colour, width);
            }
        }
    }

    private void DrawResidencyKey(Vector2 size)
    {
        var font = ThemeDB.FallbackFont;
        var origin = new Vector2(size.X - 168, 98);
        DrawRect(new Rect2(origin - new Vector2(8, 16), new Vector2(166, 18 + (ChunkOutlines.Contents.Count * 17))), new Color(0.05f, 0.06f, 0.08f, 0.8f));
        DrawString(font, origin, "Chunk residency", HorizontalAlignment.Left, -1, LabelSize, TextColour);
        for (var i = 0; i < ChunkOutlines.Contents.Count; i++)
        {
            var content = ChunkOutlines.Contents[i];
            var row = origin + new Vector2(0, 17 * (i + 1));
            DrawRect(new Rect2(row + new Vector2(0, -10), new Vector2(22, 12)), ChunkOutlines.ColourOf(content), filled: false, width: 2);
            DrawString(font, row + new Vector2(30, 0), ChunkOutlines.NameOf(content), HorizontalAlignment.Left, -1, LabelSize, TextColour);
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

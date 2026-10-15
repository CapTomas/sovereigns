using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.State;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Atlas;

/// <summary>Kinds of continuous linear feature. Persisted by name; append new kinds, never rename one.</summary>
public enum FeatureKind
{
    River,
    Bank,
    Shoreline,
    Ridgeline,
    Road,

    /// <summary>Developer survey line: an annotation for exercising feature geometry, not physical state.</summary>
    SurveyLine,
}

/// <summary>
/// A continuous linear feature with a durable identity (SOV-P03-T06): its typed ID text (for example
/// <c>survey:2</c>; later <c>feature:7</c> for a river), kind, polyline in world coordinates, label and creation
/// time. Its geometry is authoritative; refinement adds points between its vertices but never moves or drops them.
/// </summary>
public sealed record MapFeature(string Id, FeatureKind Kind, IReadOnlyList<WorldPoint> Points, string Label, SimTime CreatedAt);

/// <summary>A feature near a query point: the distance to its nearest segment and the nearest point on it.</summary>
public sealed record FeatureHit(MapFeature Feature, double DistanceM, WorldPoint Nearest, int Segment);

public static class FeatureGeometry
{
    /// <summary>
    /// Refines a polyline so that no segment is longer than <paramref name="maxSegmentM"/>, by inserting evenly spaced
    /// points along each segment. Every original vertex stays, in order, so coarse and fine views share one shape.
    /// </summary>
    public static IReadOnlyList<WorldPoint> Densify(IReadOnlyList<WorldPoint> points, double maxSegmentM)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (!(maxSegmentM > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(maxSegmentM), "segment length must be positive");
        }

        var result = new List<WorldPoint>();
        for (var k = 0; k < points.Count; k++)
        {
            result.Add(points[k]);
            if (k + 1 == points.Count)
            {
                break;
            }

            var (a, b) = (points[k], points[k + 1]);
            var length = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
            var pieces = (int)Math.Ceiling(length / maxSegmentM);
            for (var step = 1; step < pieces; step++)
            {
                var t = (double)step / pieces;
                result.Add(new WorldPoint(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t));
            }
        }

        return result;
    }

    /// <summary>Nearest point on segment a–b to p, and its distance.</summary>
    public static (WorldPoint Nearest, double Distance) NearestOnSegment(WorldPoint a, WorldPoint b, WorldPoint p)
    {
        var (dx, dy) = (b.X - a.X, b.Y - a.Y);
        var lengthSquared = dx * dx + dy * dy;
        var t = lengthSquared == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared, 0, 1);
        var nearest = new WorldPoint(a.X + dx * t, a.Y + dy * t);
        var (ex, ey) = (p.X - nearest.X, p.Y - nearest.Y);
        return (nearest, Math.Sqrt(ex * ex + ey * ey));
    }
}

/// <summary>
/// The world's linear features with a spatial index over regional cells (SOV-P03-T06, T07). The index is a derived
/// cache of the features (ADR-0007), rebuilt only after the features change.
/// </summary>
internal sealed class FeatureStore
{
    private readonly List<MapFeature> _features = [];
    private readonly DerivedCache<Dictionary<(long, long), List<int>>> _index;

    public FeatureStore()
    {
        _index = new DerivedCache<Dictionary<(long, long), List<int>>>(BuildIndex, Version);
    }

    public StateVersion Version { get; } = new("features");

    public IReadOnlyList<MapFeature> Features => _features;

    public int IndexComputations => _index.Computations;

    public void Add(MapFeature feature)
    {
        _features.Add(feature);
        Version.Bump();
    }

    public bool Remove(string id)
    {
        var index = _features.FindIndex(feature => feature.Id == id);
        if (index < 0)
        {
            return false;
        }

        _features.RemoveAt(index);
        Version.Bump();
        return true;
    }

    public void Restore(IEnumerable<MapFeature> features)
    {
        _features.Clear();
        _features.AddRange(features);
        Version.Bump();
    }

    /// <summary>Features with a segment within <paramref name="radiusM"/> of <paramref name="point"/>, nearest first, then by ID.</summary>
    public IReadOnlyList<FeatureHit> Near(WorldPoint point, double radiusM)
    {
        var radius = Math.Max(0, radiusM);
        var size = Grid.CellSize(GridLevel.Regional);
        var index = _index.Value;
        var candidates = new SortedSet<int>();
        var cellsPerSide = 2 * radius / size + 2;
        if (cellsPerSide * cellsPerSide > 4 * (index.Count + 1))
        {
            candidates.UnionWith(Enumerable.Range(0, _features.Count));
        }
        else
        {
            for (var j = (long)Math.Floor((point.Y - radius) / size); j <= (long)Math.Floor((point.Y + radius) / size); j++)
            {
                for (var i = (long)Math.Floor((point.X - radius) / size); i <= (long)Math.Floor((point.X + radius) / size); i++)
                {
                    if (index.TryGetValue((i, j), out var found))
                    {
                        candidates.UnionWith(found);
                    }
                }
            }
        }

        var hits = new List<FeatureHit>();
        foreach (var candidate in candidates)
        {
            var feature = _features[candidate];
            FeatureHit? best = null;
            for (var segment = 0; segment + 1 < feature.Points.Count; segment++)
            {
                var (nearest, distance) = FeatureGeometry.NearestOnSegment(feature.Points[segment], feature.Points[segment + 1], point);
                if (distance <= radius && (best is null || distance < best.DistanceM))
                {
                    best = new FeatureHit(feature, distance, nearest, segment);
                }
            }

            if (best is not null)
            {
                hits.Add(best);
            }
        }

        hits.Sort((a, b) => a.DistanceM != b.DistanceM ? a.DistanceM.CompareTo(b.DistanceM) : string.CompareOrdinal(a.Feature.Id, b.Feature.Id));
        return hits;
    }

    private Dictionary<(long, long), List<int>> BuildIndex()
    {
        var size = Grid.CellSize(GridLevel.Regional);
        var index = new Dictionary<(long, long), List<int>>();
        for (var f = 0; f < _features.Count; f++)
        {
            var points = _features[f].Points;
            for (var segment = 0; segment + 1 < points.Count; segment++)
            {
                // Walk the segment in half-cell steps and mark each cell with its neighbours: a conservative cover
                // whose size grows with the segment's length, not with its bounding box.
                var (a, b) = (points[segment], points[segment + 1]);
                var length = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                var steps = Math.Max(1, (int)Math.Ceiling(length / (size / 2)));
                for (var step = 0; step <= steps; step++)
                {
                    var t = (double)step / steps;
                    var (ci, cj) = ((long)Math.Floor((a.X + (b.X - a.X) * t) / size), (long)Math.Floor((a.Y + (b.Y - a.Y) * t) / size));
                    for (var j = cj - 1; j <= cj + 1; j++)
                    {
                        for (var i = ci - 1; i <= ci + 1; i++)
                        {
                            if (!index.TryGetValue((i, j), out var list))
                            {
                                index[(i, j)] = list = [];
                            }

                            if (list.Count == 0 || list[^1] != f)
                            {
                                list.Add(f);
                            }
                        }
                    }
                }
            }
        }

        return index;
    }
}

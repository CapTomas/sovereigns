using Sovereigns.Simulation.Atlas;

namespace Sovereigns.Client.View;

/// <summary>One chunk to outline on the residency overlay: its key and what the cache holds for it.</summary>
public readonly record struct ChunkOutline(ChunkKey Key, ChunkContent Content);

/// <summary>
/// The chunk-residency debug overlay's content (SOV-P03-T14): which chunks the atlas cache holds right now, taken
/// from <see cref="FieldAtlas.ResidentChunks"/>, and the colour of each kind of content. Residency never changes an
/// answer (ADR-0009); this only shows what happens to be resident.
/// </summary>
public static class ChunkOutlines
{
    /// <summary>
    /// The distinct (chunk, content) pairs of the resident chunks, coarsest level first. Several fields can hold the
    /// same chunk with the same kind of content; one outline stands for them.
    /// </summary>
    public static IReadOnlyList<ChunkOutline> From(IReadOnlyList<ResidentChunk> resident)
    {
        ArgumentNullException.ThrowIfNull(resident);
        return
        [
            .. resident
                .Select(chunk => new ChunkOutline(chunk.Key, chunk.Content))
                .Distinct()
                .OrderBy(outline => outline.Key.Level)
                .ThenBy(outline => outline.Key.X)
                .ThenBy(outline => outline.Key.Y)
                .ThenBy(outline => outline.Content),
        ];
    }

    public static IReadOnlyList<ChunkContent> Contents { get; } = Enum.GetValues<ChunkContent>();

    public static string NameOf(ChunkContent content) => content.ToString().ToLowerInvariant();

    public static Color ColourOf(ChunkContent content) => content switch
    {
        ChunkContent.Native => new Color(0.35f, 0.9f, 0.45f),
        ChunkContent.Derived => new Color(0.4f, 0.65f, 1f),
        ChunkContent.Aggregate => new Color(1f, 0.7f, 0.25f),
        _ => new Color(0.95f, 0.45f, 0.95f),
    };
}

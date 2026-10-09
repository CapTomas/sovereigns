namespace Sovereigns.Simulation.Content;

/// <summary>A problem inside one definition, located by JSON path, for example <c>$.extent_m.east</c>.</summary>
public readonly record struct ContentIssue(string JsonPath, string Message);

/// <summary>A reference from one definition to another, which must exist and have the expected kind.</summary>
public readonly record struct ContentReference(string JsonPath, ContentId Target, string ExpectedKind);

/// <summary>
/// A versioned, read-only content definition loaded from <c>content/&lt;namespace&gt;/&lt;kind&gt;/&lt;name&gt;.json</c>.
/// Its C# record shape is the schema: unknown, missing, null or mistyped members fail to load.
/// </summary>
public interface IContentDefinition
{
    ContentId Id { get; }

    /// <summary>Checks that the type system cannot express, such as ranges.</summary>
    IEnumerable<ContentIssue> Validate();

    /// <summary>Every content ID this definition depends on.</summary>
    IEnumerable<ContentReference> References();
}

namespace Sovereigns.Client.Controls;

/// <summary>Builds the bottom help line from the live bindings, so rebinding changes what the player reads.</summary>
public static class HelpText
{
    public static string Build(InputBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return string.Join("   |   ", InputActions.Help.Select(entry =>
        {
            var shown = entry.Actions.Count == 1
                ? bindings.Get(entry.Actions[0]).Select(binding => binding.DisplayName)
                : entry.Actions.Select(action => bindings.Get(action)).Where(list => list.Count > 0).Select(list => list[0].DisplayName);
            return $"{entry.Label}: {string.Join(entry.Actions.Count == 1 ? " or " : "/", shown)}";
        }));
    }
}

namespace Sovereigns.Client.Controls;

/// <summary>Registers the bindings model into Godot's InputMap; the model is the only place bindings are defined.</summary>
public static class InputMapRegistrar
{
    public static void Apply(InputBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        foreach (var action in InputActions.All)
        {
            if (InputMap.HasAction(action.Name))
            {
                InputMap.ActionEraseEvents(action.Name);
            }
            else
            {
                InputMap.AddAction(action.Name);
            }

            foreach (var binding in bindings.Get(action.Name))
            {
                InputMap.ActionAddEvent(action.Name, binding.ToInputEvent());
            }
        }
    }
}

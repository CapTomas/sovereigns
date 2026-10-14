namespace Sovereigns.Client.Tests;

public sealed record TestCase(string Name, Func<Task> Run);

/// <summary>
/// Entry point of <c>godot --headless --path game res://tests/ClientTests.tscn</c>. Prints PASS or FAIL per case and
/// exits 0 only when every case passed; a hang is a failure (exit 3).
/// </summary>
public partial class ClientTestRunner : Node
{
    private const double TimeoutSeconds = 180;

    private bool _finished;

    public override void _Ready()
    {
        GetTree().CreateTimer(TimeoutSeconds).Timeout += OnTimeout;
        _ = RunAllAsync();
    }

    /// <summary>Waits for <paramref name="count"/> rendered frames.</summary>
    public async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private async Task RunAllAsync()
    {
        var failed = 0;
        var passed = 0;
        try
        {
            // A headless display server reports a 64x64 window, too small to lay the shell out; use the project's default size.
            GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
            GetWindow().ContentScaleSize = new Vector2I(1600, 900);
            await Frames(2);
            var cases = ViewTests.Cases().Concat(LaunchTests.Cases()).Concat(PacingTests.Cases()).Concat(InputTests.Cases()).Concat(new ShellTests(this).Cases());
            foreach (var testCase in cases)
            {
                try
                {
                    await testCase.Run();
                    passed++;
                    GD.Print($"PASS {testCase.Name}");
                }
#pragma warning disable CA1031 // Any exception, including an unexpected one, fails only its own case.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    failed++;
                    GD.Print($"FAIL {testCase.Name}: {ex.Message}");
                    if (ex is not CheckFailedException)
                    {
                        GD.Print(ex.ToString());
                    }
                }
            }
        }
#pragma warning disable CA1031 // The runner itself failed; report and exit nonzero.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            failed++;
            GD.Print($"FAIL test runner: {ex}");
        }

        GD.Print($"{passed} passed, {failed} failed");
        Finish(failed == 0 ? 0 : 1);
    }

    private void OnTimeout()
    {
        GD.Print($"FAIL timeout: the client tests did not finish within {TimeoutSeconds} s");
        Finish(3);
    }

    private void Finish(int exitCode)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        GetTree().Quit(exitCode);
    }
}

using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Persistence;
using Sovereigns.Simulation.Random;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

/// <summary>
/// SOV-P02-T15: the same seed and command stream reproduce identical state checksums, whether run again, replayed
/// from the journal, or saved, loaded and continued part-way. Command streams are generated from seeded streams,
/// so every case is itself reproducible.
/// </summary>
public sealed class DeterminismTests
{
    private const long Day = WorldCalendar.MillisecondsPerDay;
    private static readonly ScenarioDefinition Scenario = TestSupport.Scenario(east: 50_000, north: 40_000, start: new CalendarStart(1202, 11, 15, 6));

    public static TheoryData<ulong> Seeds => new() { 1, 2, 3, 20261014, ulong.MaxValue };

    [Theory]
    [MemberData(nameof(Seeds))]
    public void RepeatedRunsReplaysAndSaveLoadContinuationsAgree(ulong seed)
    {
        var end = Scenario.StartTime.Plus(90 * Day);
        var first = Drive(World.Create(Scenario, seed), seed, end);
        var second = Drive(World.Create(Scenario, seed), seed, end);
        var replayed = Replay.Run(Scenario, seed, first.Journal.Entries, end);

        Assert.Equal(first.ComputeChecksum(), second.ComputeChecksum());
        Assert.Equal(first.Journal.Entries, second.Journal.Entries);
        Assert.Equal(first.ComputeChecksum(), replayed.ComputeChecksum());
        Assert.Equal(first.Ledger.Events.Count, replayed.Ledger.Events.Count);

        var splitDay = (long)RandomStream.For(seed, "test.split").NextBelow(89) + 1;
        var split = Scenario.StartTime.Plus(splitDay * Day);
        var beforeSplit = first.Journal.Entries.Where(entry => entry.At < split).ToList();
        var partial = Replay.Run(Scenario, seed, beforeSplit, split);
        using var save = new MemoryStream();
        SaveFile.Capture(partial).WriteTo(save);
        var loaded = SaveFile.Load(new MemoryStream(save.ToArray()));
        var continued = Replay.Continue(loaded, [.. first.Journal.Entries.Skip(beforeSplit.Count)], end);

        Assert.Equal(first.ComputeChecksum(), continued.ComputeChecksum());
    }

    /// <summary>
    /// Submits a seeded mix of commands at random step-aligned times: markers placed and removed, breakpoints that
    /// stop time, acknowledgements, time controls and some deliberately invalid commands.
    /// </summary>
    private static World Drive(World world, ulong seed, SimTime end)
    {
        var random = RandomStream.For(seed, "test.commands");
        while (world.Now < end)
        {
            if (world.PendingDecisions.Count > 0)
            {
                world.Submit(new AcknowledgeDecision(world.PendingDecisions[0].Event));
                continue;
            }

            var jump = (long)random.NextBelow(36 * 3_600) + 1;
            var target = world.Now.Plus(Math.Min(jump * 1_000, end.Milliseconds - world.Now.Milliseconds));
            world.AdvanceTo(target);
            if (world.PendingDecisions.Count > 0)
            {
                continue;
            }

            switch (random.NextBelow(7))
            {
                case 0 or 1:
                    world.Submit(new PlaceMarker(new WorldPoint(random.NextDouble() * 55_000, random.NextDouble() * 40_000), $"m{random.NextBelow(1000)}"));
                    break;
                case 2:
                    world.Submit(new RemoveMarker(new EntityId<Kind.Marker>(random.NextBelow(world.Ids.LastIssued<Kind.Marker>() + 2))));
                    break;
                case 3:
                    world.Submit(new SetBreakpoint(world.Now.Plus(((long)random.NextBelow(5 * 86_400) + 1) * 1_000), "auto"));
                    break;
                case 4:
                    world.Control(new SetSpeed(TimeControl.Presets[(int)random.NextBelow(5)]));
                    break;
                case 5:
                    world.Submit(new AcknowledgeDecision(new EntityId<Kind.Event>(random.NextBelow(50))));
                    break;
                default:
                    world.Control(random.NextChance(0.5) ? new Pause() : new Resume());
                    break;
            }
        }

        return world;
    }
}

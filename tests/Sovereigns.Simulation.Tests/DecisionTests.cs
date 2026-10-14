using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

/// <summary>SOV-P02-T11: pause, step, accelerated stepping and non-skippable decision interrupts.</summary>
public sealed class DecisionTests
{
    private const long Day = WorldCalendar.MillisecondsPerDay;

    [Fact]
    public void ADecisionStopsTimeUntilAcknowledged()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);
        world.Submit(new SetBreakpoint(new SimTime(3 * Day), "muster"));

        var reached = world.AdvanceTo(new SimTime(30 * Day));

        Assert.Equal(new SimTime(3 * Day), reached);
        var decision = Assert.Single(world.PendingDecisions);
        Assert.Equal("muster", decision.Label);
        Assert.Equal(new SimTime(3 * Day), world.AdvanceTo(new SimTime(30 * Day)));
        Assert.Equal(new SimTime(3 * Day), world.Step());

        world.Submit(new AcknowledgeDecision(decision.Event));

        Assert.Equal(new SimTime(30 * Day), world.AdvanceTo(new SimTime(30 * Day)));
        var raised = world.Ledger.Events.Single(e => e.Type == WorldEventType.DecisionRaised);
        var set = world.Ledger.Events.Single(e => e.Type == WorldEventType.BreakpointSet);
        Assert.Equal(EventCause.FromEvent(set.Id), raised.Cause);
        Assert.Equal(CauseKind.Command, world.Ledger.Events.Single(e => e.Type == WorldEventType.DecisionAcknowledged).Cause.Kind);
    }

    [Fact]
    public void StepRequestsDoNotSurviveADecision()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);
        world.Submit(new SetBreakpoint(new SimTime(2_000), "stop"));
        world.Control(new StepOnce());
        world.Control(new StepOnce());
        world.Control(new StepOnce());
        var pacer = new RealTimePacer();

        pacer.Advance(world, 0);
        pacer.Advance(world, 0);

        Assert.Equal(new SimTime(2_000), world.Now);
        Assert.Equal(0, world.TimeControl.RequestedSteps);
        Assert.NotNull(world.Control(new StepOnce()));
        world.Submit(new AcknowledgeDecision(world.PendingDecisions[0].Event));
        world.Control(new Pause());
        Assert.Equal(new SimTime(2_000), pacer.Advance(world, 1.0));
    }

    [Fact]
    public void SimultaneousDecisionsMustAllBeAcknowledged()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);
        world.Submit(new SetBreakpoint(new SimTime(Day), "a"));
        world.Submit(new SetBreakpoint(new SimTime(Day), "b"));
        world.AdvanceTo(new SimTime(2 * Day));

        Assert.Equal(["a", "b"], world.PendingDecisions.Select(d => d.Label));
        world.Submit(new AcknowledgeDecision(world.PendingDecisions[0].Event));
        Assert.Equal(new SimTime(Day), world.AdvanceTo(new SimTime(2 * Day)));
        world.Submit(new AcknowledgeDecision(world.PendingDecisions[0].Event));
        Assert.Equal(new SimTime(2 * Day), world.AdvanceTo(new SimTime(2 * Day)));
    }

    [Fact]
    public void PacerFollowsSpeedAndPauseAndStepRequests()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);
        var pacer = new RealTimePacer();

        Assert.Equal(new SimTime(1_000), pacer.Advance(world, 1.0));
        world.Control(new SetSpeed(TimeControl.Presets[2]));
        Assert.Equal(new SimTime(1_000 + 900_000), pacer.Advance(world, 0.25));
        world.Control(new Pause());
        Assert.Equal(new SimTime(901_000), pacer.Advance(world, 5.0));
        world.Control(new StepOnce());
        Assert.Equal(new SimTime(902_000), pacer.Advance(world, 0.0));
        Assert.Equal(new SimTime(902_000), pacer.Advance(world, 5.0));
        world.Control(new Resume());
        Assert.Equal(new SimTime(902_000 + 36_000), pacer.Advance(world, 0.01));
    }

    [Fact]
    public void PacerCapsBacklogAfterAHitchAndRespectsLimits()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);
        var pacer = new RealTimePacer();

        // At real-time speed a 10 s hitch catches up at most one 1 s step, not ten.
        Assert.Equal(new SimTime(1_000), pacer.Advance(world, 10.0));
        Assert.Equal(1, pacer.DroppedBacklogCount);

        world.Control(new SetSpeed(TimeControl.Presets[2]));
        Assert.Equal(new SimTime(3_000), pacer.Advance(world, 0.25, limit: new SimTime(3_000)));
        Assert.Equal(new SimTime(3_000), pacer.Advance(world, 0.25, limit: new SimTime(3_000)));
    }

    [Fact]
    public void EvenAWeekPerSecondCannotSkipADecision()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);
        world.Control(new SetSpeed(TimeControl.Presets[4]));
        world.Submit(new SetBreakpoint(new SimTime(Day + 5_000), "dawn check"));
        var pacer = new RealTimePacer();

        for (var frame = 0; frame < 120; frame++)
        {
            pacer.Advance(world, 1.0 / 60);
        }

        Assert.Equal(new SimTime(Day + 5_000), world.Now);
        Assert.Single(world.PendingDecisions);
    }
}

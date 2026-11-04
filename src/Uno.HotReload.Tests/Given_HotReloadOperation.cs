using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Tests;

[TestClass]
public class Given_HotReloadOperation
{
	[TestMethod]
	[Description(
		"Completing an already-completed operation keeps the first result (immutable outcome) " +
		"but must report the dropped completion through the reporter instead of returning " +
		"silently — a swallowed Failed-with-diagnostics is how broken content gets reported " +
		"as a clean hot-reload.")]
	public async Task When_CompletingAnAlreadyCompletedOperation_Then_DroppedResultIsReported()
	{
		var reporter = new RecordingReporter();
		var tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: reporter);

		var operation = await tracker.StartHotReload(ImmutableHashSet.Create("/work/Page.xaml"));
		await operation.Complete(HotReloadOperationResult.Success);

		var diagnostic = Diagnostic.Create(
			new DiagnosticDescriptor(
				"ENC0033",
				"Rude edit",
				"Deleting a method requires restarting the application",
				"EditAndContinue",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true),
			Location.None);
		await operation.Complete(HotReloadOperationResult.Failed, diagnostics: ImmutableArray.Create(diagnostic));

		// First completion wins — the operation's outcome is immutable.
		operation.Result.Should().Be(HotReloadOperationResult.Success);
		operation.Diagnostics.Should().BeNull();

		// But the dropped completion must leave a trace.
		reporter.Warnings.Should().Contain(
			w => w.Contains("already completed"),
			"a late Complete(Failed) carries information that must not vanish silently");
	}

	[TestMethod]
	[Description(
		"Chain-abort completions (a newer operation completing marks its predecessors Aborted) " +
		"are routine lifecycle management, not lost results — they must not produce warnings.")]
	public async Task When_PreviousOperationAbortedByNext_Then_NoWarningIsReported()
	{
		var reporter = new RecordingReporter();
		var tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: reporter);

		var first = await tracker.StartHotReload(ImmutableHashSet.Create("/work/A.cs"));
		var second = await tracker.StartHotReload(ImmutableHashSet.Create("/work/B.cs"));

		// First completes on its own; the newer operation's completion then walks the
		// chain and re-completes it as Aborted — a routine, ignorable late completion.
		await first.Complete(HotReloadOperationResult.Success);
		await second.Complete(HotReloadOperationResult.Success);

		first.Result.Should().Be(HotReloadOperationResult.Success);
		reporter.Warnings.Should().BeEmpty("chain-aborting an already-completed predecessor is routine, not a dropped result");
	}

	[TestMethod]
	[Description(
		"OperationCompleted is raised once per operation, after its result and completion time are set; " +
		"a late completion the operation drops raises nothing.")]
	public async Task When_OperationCompletes_Then_OperationCompletedIsRaisedOnceWithItsResult()
	{
		var tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: new RecordingReporter());
		var raised = new List<(HotReloadOperation Operation, HotReloadOperationResult? Result, DateTimeOffset? CompletionTime)>();
		tracker.OperationCompleted += (_, operation) => raised.Add((operation, operation.Result, operation.CompletionTime));

		var operation = await tracker.StartHotReload(ImmutableHashSet.Create("/work/Page.xaml"));
		await operation.Complete(HotReloadOperationResult.Success);
		await operation.Complete(HotReloadOperationResult.Failed);

		var (raisedOperation, result, completionTime) = raised.Should().ContainSingle().Which;
		raisedOperation.Should().BeSameAs(operation);
		result.Should().Be(HotReloadOperationResult.Success);
		completionTime.Should().NotBeNull();
	}

	[TestMethod]
	[Description("A newer operation completing aborts its predecessor: both operations are raised, each once.")]
	public async Task When_NewerOperationCompletes_Then_EachOperationIsRaisedOnce()
	{
		var tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: new RecordingReporter());
		var raised = new List<(long Id, HotReloadOperationResult? Result)>();
		tracker.OperationCompleted += (_, operation) => raised.Add((operation.Id, operation.Result));

		var first = await tracker.StartHotReload(ImmutableHashSet.Create("/work/A.cs"));
		var second = await tracker.StartHotReload(ImmutableHashSet.Create("/work/B.cs"));
		await second.Complete(HotReloadOperationResult.Success);

		raised.Should().Equal(
			(first.Id, HotReloadOperationResult.Aborted),
			(second.Id, HotReloadOperationResult.Success));
	}

	[TestMethod]
	[Description(
		"A failing OperationCompleted handler (e.g. telemetry) must not fail the hot reload: the operation " +
		"completes as usual and the handler's exception is reported.")]
	public async Task When_OperationCompletedHandlerThrows_Then_OperationStillCompletesAndErrorIsReported()
	{
		var reporter = new RecordingReporter();
		var tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: reporter);
		tracker.OperationCompleted += (_, _) => throw new InvalidOperationException("handler failure");

		var operation = await tracker.StartHotReload(ImmutableHashSet.Create("/work/Page.xaml"));
		await operation.Complete(HotReloadOperationResult.Success);

		operation.Result.Should().Be(HotReloadOperationResult.Success);
		await operation.WaitForCompletionAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
		reporter.Errors.Should().ContainSingle().Which.Should().Contain("handler failure");
	}
}

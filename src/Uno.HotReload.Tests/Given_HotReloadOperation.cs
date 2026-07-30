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
}

using System.Collections.Immutable;
using System.Text;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Uno.HotReload.Diffing;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Tests;

/// <summary>
/// Issue #25418 through the real EnC pipeline: Roslyn throws while emitting the delta of an edit
/// (dotnet/roslyn#86045). The edit can't be applied, but the session is not broken: it must be
/// reported as a rude edit the user can undo, not as an internal error.
/// </summary>
[TestClass]
public sealed class Given_HotReloadManager_EmitFailure
{
	// dotnet/roslyn#86045: a lambda already captures the dynamic local, and the edit adds an async
	// lambda that also uses it; the delta writer can't resolve the synthesized call-site field.
	private const string Baseline = """
		using System;
		using System.Collections.Generic;
		using System.Threading;
		using System.Threading.Tasks;

		public class Model
		{
			public int Count;
			public async Task Increment(CancellationToken ct) { await Task.Yield(); Count++; }
		}

		public class ViewModel
		{
			public readonly List<Func<object>> Feeds = new();
			public readonly List<Func<CancellationToken, Task>> Commands = new();

			public void Initialize(object updatedModel)
			{
				dynamic model = updatedModel;
				Feeds.Clear(); Commands.Clear();
				Feeds.Add(() => model.Count);
				// COMMAND
			}

			public static string Marker() => "v1";
		}
		""";

	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[Description(
		"A compiler exception during the emit (dotnet/roslyn#86045) completes the operation as a " +
		"RudeEdit with a diagnostic telling the user to undo or restart. Undoing the edit is a " +
		"NoChanges (the EnC baseline never moved) and a later valid edit applies.")]
	public async Task When_EmitThrows_Then_RudeEdit_AndUndoRecovers()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
		using var enc = await EnCHarness.CreateAsync(
			temp,
			ct,
			SourceText.From(Baseline, Encoding.UTF8),
			[
				// What `dynamic` binds against.
				MetadataReference.CreateFromFile(typeof(global::Microsoft.CSharp.RuntimeBinder.Binder).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.CallSite).Assembly.Location),
				MetadataReference.CreateFromFile(Path.Join(runtimeDir, "System.Runtime.dll")),
			]);

		string[] edits =
		[
			Baseline.Replace("// COMMAND", "Commands.Add(async ct => await model.Increment(ct));", StringComparison.Ordinal),
			Baseline,
			Baseline.Replace("\"v1\"", "\"v2\"", StringComparison.Ordinal),
		];
		var cycle = 0;
		var updater = new DelegateSolutionUpdater(solution => new SolutionUpdateResult(
			solution.WithDocumentText(enc.DocumentId, SourceText.From(edits[cycle++], Encoding.UTF8)),
			ChangeSet.IgnoreAll([])));

		var reporter = new RecordingReporter();
		var tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: reporter);
		var handler = new HotReloadManagerHarness.RecordingHandler();
		enc.OwnsSession = false; // the manager's Dispose ends the EnC session
		using var manager = new HotReloadManager(
			enc.Workspace,
			enc.Watch,
			handler,
			new DelegateChangesDetector(),
			updater,
			tracker,
			enc.Solution);
		var files = ImmutableHashSet.Create(enc.Solution.GetDocument(enc.DocumentId)!.FilePath!);

		// 1. The crashing edit.
		await manager.ProcessFileChanges(Task.FromResult(files), ct);

		tracker.Last!.Result.Should().Be(
			HotReloadOperationResult.RudeEdit,
			"a compiler crash while emitting the delta means this edit can't be applied, not that the DevServer failed");
		var (result, update) = handler.Calls.Should().ContainSingle().Subject;
		result.Should().Be(HotReloadOperationResult.RudeEdit);
		update.Deltas.Should().BeEmpty();
		var diagnostic = update.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
		diagnostic.Id.Should().Be("ENC1002");
		diagnostic.GetMessage().Should().StartWith("The compiler could not produce a Hot Reload update for this change (Unexpected value '");
		diagnostic.GetMessage().Should().EndWith("). Undo the change to keep going, or restart the app.");
		reporter.Warnings.Should().Contain(
			w => w.Contains("InvalidOperationException", StringComparison.Ordinal) && w.Contains("DeltaMetadataWriter", StringComparison.Ordinal),
			"the compiler bug must stay visible in the log, with its stack");

		// 2. Undo the edit.
		await manager.ProcessFileChanges(Task.FromResult(files), ct);

		tracker.Last!.Result.Should().Be(HotReloadOperationResult.NoChanges, "the EnC baseline never moved past the crashing edit");

		// 3. A valid edit.
		await manager.ProcessFileChanges(Task.FromResult(files), ct);

		tracker.Last!.Result.Should().Be(HotReloadOperationResult.Success, "the session survives the crash");
		handler.Calls.Should().HaveCount(3);
		handler.Calls[^1].Update.Deltas.Should().HaveCount(1);
	}

	private sealed class DelegateChangesDetector : IChangesDetector
	{
		public ValueTask<ChangeSet> DiscoverChangesAsync(Solution solution, ImmutableHashSet<string> files, CancellationToken ct)
			=> ValueTask.FromResult(ChangeSet.IgnoreAll([]));
	}

	private sealed class DelegateSolutionUpdater(Func<Solution, SolutionUpdateResult> update) : ISolutionUpdater
	{
		public ValueTask<SolutionUpdateResult> UpdateAsync(Solution solution, ChangeSet changeSet, CancellationToken ct)
			=> ValueTask.FromResult(update(solution));
	}
}

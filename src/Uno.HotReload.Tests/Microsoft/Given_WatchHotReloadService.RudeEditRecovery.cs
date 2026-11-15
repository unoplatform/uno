using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tests.TestUtils;

namespace Uno.HotReload.Tests.Microsoft;

/// <summary>
/// Whether an EnC session recovers from a rejected rude edit, in plain C# (the <c>When_Base_Type_*</c>
/// hot-reload scenarios assert the same through the XAML generator). Roslyn 5.x answers <c>Ready</c>
/// for a rude edit, so these only discriminate on that line.
/// </summary>
[TestClass]
public sealed class Given_WatchHotReloadService_RudeEditRecovery
{
	public TestContext TestContext { get; set; } = null!;

	private const string BaseTypes = """
		public class BaseA { }
		public class BaseB { }
		""";

	/// <summary>Error diagnostics, rendered so a failure names what was actually reported.</summary>
	private static string[] Errors(ImmutableArray<Diagnostic> diagnostics)
		=> diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.Select(d => $"{d.Id}: {d.GetMessage()}")
			.ToArray();

	private static string AppWith(string baseType) => $$"""
		{{BaseTypes}}
		public partial class C : {{baseType}}
		{
			public string M() => "v1";
		}
		""";

	[TestMethod]
	[Description(
		"Changing a class's base type is a rude edit (ENC0014) and must emit no delta. Guards the " +
		"premise of the recovery test below: the first pass of the When_Base_Type_* scenarios " +
		"expects exactly this.")]
	public async Task When_BaseTypeChanged_Then_RudeEditAndNoDelta()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(temp, ct, EnCHarness.RawText(AppWith("BaseA")));

		var changed = harness.Solution.WithDocumentText(harness.DocumentId, EnCHarness.RawText(AppWith("BaseB")));

		var emit = await harness.Watch.EmitSolutionUpdateAsync(changed, ct);

		emit.Deltas.Should().BeEmpty("a base-type change cannot be applied to a non-reloadable type");
		emit.Diagnostics.Should().Contain(d => d.Id == "ENC0014");
		emit.RequiresRebuildOrRestart.Should().BeTrue(
			"the project can no longer be brought in line with its sources by hot reload alone");
	}

	[TestMethod]
	[Description(
		"After the rude edit above is rejected, restoring the ORIGINAL source must leave nothing to " +
		"report: the solution is byte-identical to the session baseline again. This is the second " +
		"pass of the When_Base_Type_* scenarios.")]
	public async Task When_BaseTypeChangeReverted_Then_SessionIsCleanAgain()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(temp, ct, EnCHarness.RawText(AppWith("BaseA")));

		// Pass 1: the rude edit, rejected.
		var broken = harness.Solution.WithDocumentText(harness.DocumentId, EnCHarness.RawText(AppWith("BaseB")));
		var rejected = await harness.Watch.EmitSolutionUpdateAsync(broken, ct);
		rejected.Diagnostics.Should().Contain(d => d.Id == "ENC0014", "precondition: pass 1 is the rude edit");

		// Pass 2: back to the baseline content.
		var reverted = broken.WithDocumentText(harness.DocumentId, EnCHarness.RawText(AppWith("BaseA")));
		var emit = await harness.Watch.EmitSolutionUpdateAsync(reverted, ct);

		Errors(emit.Diagnostics).Should().BeEmpty(
			"the source matches the baseline again, so there is nothing left to reject");
		emit.Deltas.Should().BeEmpty("identical content produces no delta");
		emit.RequiresRebuildOrRestart.Should().BeFalse();
	}

	[TestMethod]
	[Description(
		"Re-emitting the SAME rejected rude edit must keep reporting it: the source still differs " +
		"from the session baseline, and nothing was ever applied. Going quiet instead would mean the " +
		"session moved its comparison point onto a solution it never applied.")]
	public async Task When_SameRudeEditEmittedTwice_Then_StillReported()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(temp, ct, EnCHarness.RawText(AppWith("BaseA")));

		var broken = harness.Solution.WithDocumentText(harness.DocumentId, EnCHarness.RawText(AppWith("BaseB")));

		var first = await harness.Watch.EmitSolutionUpdateAsync(broken, ct);
		first.Diagnostics.Should().Contain(d => d.Id == "ENC0014", "precondition: the first emit rejects the edit");

		// Same snapshot, unchanged: still BaseB against a BaseA baseline.
		var second = await harness.Watch.EmitSolutionUpdateAsync(broken, ct);

		second.Diagnostics.Should().Contain(
			d => d.Id == "ENC0014",
			"the source still does not match the baseline, so the rude edit stands");
	}

	[TestMethod]
	[Description(
		"Same revert, but with an applicable edit in between instead of a rude one — the control. " +
		"Isolates 'the session cannot recover' from 'the session cannot recover FROM A RUDE EDIT'.")]
	public async Task When_ApplicableEditReverted_Then_SessionIsCleanAgain()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(temp, ct, EnCHarness.RawText(AppWith("BaseA")));

		var edited = harness.Solution.WithDocumentText(
			harness.DocumentId,
			EnCHarness.RawText(AppWith("BaseA").Replace("\"v1\"", "\"v2\"")));
		var applied = await harness.Watch.EmitSolutionUpdateAsync(edited, ct);

		Errors(applied.Diagnostics).Should().BeEmpty("precondition: changing a method body is applicable");
		applied.Deltas.Should().HaveCount(1, "precondition: the applicable edit produces a delta");
		applied.Status.Should().Be(HotReloadEmitStatus.Ready);

		var again = await harness.Watch.EmitSolutionUpdateAsync(edited, ct);
		again.Deltas.Should().BeEmpty("the applied edit was committed, so it is the baseline now");
		Errors(again.Diagnostics).Should().BeEmpty();

		var reverted = edited.WithDocumentText(harness.DocumentId, EnCHarness.RawText(AppWith("BaseA")));
		var emit = await harness.Watch.EmitSolutionUpdateAsync(reverted, ct);

		Errors(emit.Diagnostics).Should().BeEmpty();
	}

	[TestMethod]
	[Description(
		"Issue #25378, on a session that already applied an update (a fresh session hides the lost " +
		"baseline): an edit made while the rude edit is still in the source stays rejected instead of " +
		"emitting a delta against the rejected text, and reverting the rude edit applies the pending edit.")]
	public async Task When_EditedWhileRudeEditPending_Then_StaysRejectedUntilReverted()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(temp, ct, EnCHarness.RawText(AppWith("BaseA")));

		Task<HotReloadEmitResult> Emit(string baseType, string value)
			=> harness.Watch.EmitSolutionUpdateAsync(
				harness.Solution.WithDocumentText(harness.DocumentId, EnCHarness.RawText(AppWith(baseType).Replace("\"v1\"", $"\"{value}\""))),
				ct);

		var applied = await Emit("BaseA", "v2");
		Errors(applied.Diagnostics).Should().BeEmpty("precondition: changing a method body is applicable");
		applied.Deltas.Should().HaveCount(1);

		var rude = await Emit("BaseB", "v2");
		rude.Deltas.Should().BeEmpty();
		Errors(rude.Diagnostics).Should().Contain(e => e.StartsWith("ENC0014", StringComparison.Ordinal));

		var stillRude = await Emit("BaseB", "v3");
		stillRude.Deltas.Should().BeEmpty("the rejected base-type change is still in the source");
		Errors(stillRude.Diagnostics).Should().Contain(e => e.StartsWith("ENC0014", StringComparison.Ordinal));

		var reverted = await Emit("BaseA", "v3");
		Errors(reverted.Diagnostics).Should().BeEmpty("the revert returns to the last applied update, not to the rejected one");
		reverted.Deltas.Should().HaveCount(1);
	}

	[TestMethod]
	[Description(
		"An applicable edit in one project and a rude edit in another: nothing is handed out, and " +
		"reverting the rude edit applies the pending edit. Committing the mixed emit would have moved " +
		"the rejected project's baseline along with it.")]
	public async Task When_RudeEditInOtherProject_Then_NothingAppliedUntilReverted()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(
			temp,
			ct,
			EnCHarness.RawText(AppWith("BaseA")),
			otherText: EnCHarness.RawText(AppWith("BaseA")));
		var otherDocumentId = harness.OtherDocumentId!;

		Solution Edit(string otherBaseType)
			=> harness.Solution
				.WithDocumentText(harness.DocumentId, EnCHarness.RawText(AppWith("BaseA").Replace("\"v1\"", "\"v2\"")))
				.WithDocumentText(otherDocumentId, EnCHarness.RawText(AppWith(otherBaseType)));

		var mixed = await harness.Watch.EmitSolutionUpdateAsync(Edit("BaseB"), ct);
		mixed.Diagnostics.Should().Contain(d => d.Id == "ENC0014", "precondition: Other carries the rude edit");
		mixed.Deltas.Should().BeEmpty("an emit that is not fully applicable hands out nothing");
		mixed.RequiresRebuildOrRestart.Should().BeTrue();

		var reverted = await harness.Watch.EmitSolutionUpdateAsync(Edit("BaseA"), ct);
		Errors(reverted.Diagnostics).Should().BeEmpty("Other matches its baseline again");
		reverted.Deltas.Should().HaveCount(1, "App's edit was never committed, so it is still pending");
		reverted.RequiresRebuildOrRestart.Should().BeFalse();
	}
}

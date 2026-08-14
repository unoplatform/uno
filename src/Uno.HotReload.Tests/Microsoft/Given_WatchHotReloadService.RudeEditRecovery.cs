using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tests.TestUtils;

namespace Uno.HotReload.Tests.Microsoft;

/// <summary>
/// Whether an EnC session RECOVERS from a rejected rude edit: after an emit that produced no delta
/// because the edit was not applicable, reverting the source back to the baseline must leave the
/// session clean again. The four <c>When_Base_Type_*</c> hot-reload scenarios assert exactly that
/// (their second pass expects zero diagnostics); these tests isolate the behavior from the XAML
/// generator, in plain C#.
/// </summary>
/// <remarks>
/// Two of them failed before the shim took over the commit decision. On the Roslyn 5.x line a rude
/// edit answers <c>ModuleUpdateStatus.Ready</c> with zero updates (4.x answered
/// <c>RestartRequired</c>), so <c>UnitTestingHotReloadService</c>'s
/// <c>if (Status == Ready) CommitSolutionUpdate(…)</c> committed the REJECTED solution as the
/// baseline of the next emit.
/// </remarks>
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
		using var harness = await EnCHarness.CreateAsync(temp, ct, AppWith("BaseA"));

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
		using var harness = await EnCHarness.CreateAsync(temp, ct, AppWith("BaseA"));

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
		using var harness = await EnCHarness.CreateAsync(temp, ct, AppWith("BaseA"));

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
		using var harness = await EnCHarness.CreateAsync(temp, ct, AppWith("BaseA"));

		var edited = harness.Solution.WithDocumentText(
			harness.DocumentId,
			EnCHarness.RawText(AppWith("BaseA").Replace("\"v1\"", "\"v2\"")));
		var applied = await harness.Watch.EmitSolutionUpdateAsync(edited, ct);

		Errors(applied.Diagnostics).Should().BeEmpty("precondition: changing a method body is applicable");
		applied.Deltas.Should().HaveCount(1, "precondition: the applicable edit produces a delta");
		applied.Status.Should().Be(HotReloadEmitStatus.Ready);

		var reverted = edited.WithDocumentText(harness.DocumentId, EnCHarness.RawText(AppWith("BaseA")));
		var emit = await harness.Watch.EmitSolutionUpdateAsync(reverted, ct);

		Errors(emit.Diagnostics).Should().BeEmpty();
	}
}

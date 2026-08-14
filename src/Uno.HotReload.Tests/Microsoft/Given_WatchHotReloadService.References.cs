using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tests.TestUtils;

namespace Uno.HotReload.Tests.Microsoft;

/// <summary>
/// Documents how the embedded Roslyn's EnC treats metadata-reference changes between the
/// session baseline and the emitted solution (issue #24023). Roslyn 5.x allows referencing a
/// brand-new assembly (the update is emitted and the project is marked for redeploy) but
/// blocks the emit with ENC1099 when the identity of an already-referenced assembly changes —
/// the exact shape a mid-session <c>PackageReference</c> add produces when the package's
/// transitive closure re-binds an assembly the app was built against at another version.
/// These tests are the shim-level canary: if a Roslyn update relaxes the behavior, they fail
/// and the manager-level identity pinning can be reconsidered.
/// </summary>
[TestClass]
public sealed class Given_WatchHotReloadService_References
{
	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[Description(
		"Referencing an assembly the baseline never knew (a plain package add) is NOT a rude " +
		"edit for Roslyn 5.x: the delta is emitted with the new AssemblyRef. Guards the " +
		"supported half of the mid-session package-add scenario.")]
	public async Task When_NewAssemblyReferenceAdded_Then_UpdateIsEmitted()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(temp, ct);

		var newLib = EnCHarness.EmitLibrary(temp, "FreshLib", "1.0.0.0", """
			namespace Fresh;
			public static class Info
			{
				public static string Name => "fresh";
			}
			""");

		var changed = harness.Solution
			.AddMetadataReference(harness.ProjectId, MetadataReference.CreateFromFile(newLib))
			.WithDocumentText(harness.DocumentId, EnCHarness.AppText("Fresh.Info.Name"));

		var (updates, diagnostics, _) = await harness.Watch.EmitSolutionUpdateAsync(changed, ct);

		diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
		updates.Should().HaveCount(1, "the edit compiles against the added assembly and must produce a delta");
	}

	[TestMethod]
	[Description(
		"Re-binding an already-referenced assembly to another version (what a package add does " +
		"when its transitive closure overlaps the app's graph) makes Roslyn 5.x refuse the " +
		"whole emit with ENC1099 — 0 deltas. This is the raw behavior behind issue #24023; the " +
		"manager neutralizes it by pinning identities to the baseline before the emit.")]
	public async Task When_ReferencedAssemblyIdentityChanges_Then_EmitIsBlockedWithENC1099()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(temp, ct);

		// Same simple name as the baseline's ConflictLib, different version, different file.
		var v2 = EnCHarness.EmitLibrary(temp, "ConflictLib", "2.0.0.0", EnCHarness.ConflictLibSource, subdir: "v2");

		var project = harness.Solution.GetProject(harness.ProjectId)!;
		var rebound = project.MetadataReferences
			.Where(r => r is not PortableExecutableReference { FilePath: { } p } || !p.EndsWith("ConflictLib.dll", StringComparison.Ordinal))
			.Append(MetadataReference.CreateFromFile(v2))
			.ToImmutableArray();

		var changed = harness.Solution
			.WithProjectMetadataReferences(harness.ProjectId, rebound)
			.WithDocumentText(harness.DocumentId, EnCHarness.AppText("\"v:\" + Conflict.Info.Version"));

		var (updates, diagnostics, _) = await harness.Watch.EmitSolutionUpdateAsync(changed, ct);

		updates.Should().BeEmpty();
		diagnostics.Should().Contain(
			d => d.Id == "ENC1099",
			"changing the identity of a referenced assembly is a project-level rude edit for Roslyn 5.x");
	}
}

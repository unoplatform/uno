using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Tests.Utils;

/// <summary>
/// Unit coverage of the reference-identity pinning primitives
/// (<see cref="RoslynExtensions.SnapshotReferenceIdentities"/> /
/// <see cref="RoslynExtensions.WithBaselineReferenceIdentities"/>) the manager uses to
/// neutralize Roslyn 5.x's ENC1099 on re-bound references (#24023).
/// </summary>
[TestClass]
public sealed class Given_ReferenceIdentityPinning
{
	private static TempDirectory _temp = null!;
	private static string _v1 = null!;
	private static string _v2 = null!;
	private static string _fresh = null!;

	[ClassInitialize]
	public static void ClassInitialize(TestContext _)
	{
		_temp = new TempDirectory();
		_v1 = EnCHarness.EmitLibrary(_temp, "ConflictLib", "1.0.0.0", EnCHarness.ConflictLibSource, subdir: "v1");
		_v2 = EnCHarness.EmitLibrary(_temp, "ConflictLib", "2.0.0.0", EnCHarness.ConflictLibSource, subdir: "v2");
		_fresh = EnCHarness.EmitLibrary(_temp, "FreshLib", "1.0.0.0", "namespace Fresh; public static class Info { }");
	}

	[ClassCleanup]
	public static void ClassCleanup()
		=> _temp.Dispose();

	private static AppProject CreateProject(params MetadataReference[] references)
	{
		var workspace = new AdhocWorkspace();
		var project = workspace.AddProject("App", LanguageNames.CSharp);
		workspace.TryApplyChanges(project.Solution.WithProjectMetadataReferences(project.Id, references));
		return new AppProject(workspace, project.Id);
	}

	private static IEnumerable<string?> PathsOf(Solution solution, ProjectId projectId)
		=> solution.GetProject(projectId)!.MetadataReferences
			.OfType<PortableExecutableReference>()
			.Select(r => r.FilePath);

	[TestMethod]
	[Description(
		"A reference REPLACED by a same-named file at another path (a re-resolution) is pinned " +
		"back to the baseline file.")]
	public void When_ReferenceReplaced_Then_PinnedBackToBaseline()
	{
		using var app = CreateProject(MetadataReference.CreateFromFile(_v1));
		var baseline = app.Solution.SnapshotReferenceIdentities(out _);

		var changed = app.Solution.WithProjectMetadataReferences(
			app.ProjectId,
			[MetadataReference.CreateFromFile(_v2)]);

		var aligned = changed.WithBaselineReferenceIdentities(baseline, out var pinned);

		PathsOf(aligned, app.ProjectId).Should().BeEquivalentTo([_v1]);
		pinned.Should().ContainSingle().Which.Should().Be(
			new PinnedReference("App", "ConflictLib", _v2, _v1));
	}

	[TestMethod]
	[Description(
		"A same-named file added ALONGSIDE the baseline reference (a blind closure bind) is " +
		"dropped, keeping a single occurrence of the baseline file.")]
	public void When_ReferenceAddedAlongside_Then_DuplicateCollapsesToBaseline()
	{
		using var app = CreateProject(MetadataReference.CreateFromFile(_v1));
		var baseline = app.Solution.SnapshotReferenceIdentities(out _);

		var changed = app.Solution.AddMetadataReference(app.ProjectId, MetadataReference.CreateFromFile(_v2));

		var aligned = changed.WithBaselineReferenceIdentities(baseline, out var pinned);

		PathsOf(aligned, app.ProjectId).Should().BeEquivalentTo([_v1]);
		pinned.Should().ContainSingle();
	}

	[TestMethod]
	[Description("A reference whose simple name the baseline never knew — a genuine add — flows through untouched.")]
	public void When_NewNameAdded_Then_Untouched()
	{
		using var app = CreateProject(MetadataReference.CreateFromFile(_v1));
		var baseline = app.Solution.SnapshotReferenceIdentities(out _);

		var changed = app.Solution.AddMetadataReference(app.ProjectId, MetadataReference.CreateFromFile(_fresh));

		var aligned = changed.WithBaselineReferenceIdentities(baseline, out var pinned);

		aligned.Should().BeSameAs(changed, "nothing needed pinning");
		pinned.Should().BeEmpty();
		PathsOf(aligned, app.ProjectId).Should().BeEquivalentTo([_v1, _fresh]);
	}

	[TestMethod]
	[Description(
		"A name mapped to two different files in the baseline (multi-version baseline) is " +
		"excluded from pinning — Roslyn owns that case (ENC1098).")]
	public void When_BaselineHasMultiVersionName_Then_NameIsNotPinned()
	{
		using var app = CreateProject(
			MetadataReference.CreateFromFile(_v1),
			MetadataReference.CreateFromFile(_v2));
		var baseline = app.Solution.SnapshotReferenceIdentities(out var multiVersionNames);
		multiVersionNames.Should().BeEquivalentTo(["ConflictLib"], "the declined coverage must surface to the caller");

		var v3Path = EnCHarness.EmitLibrary(_temp, "ConflictLib", "3.0.0.0", EnCHarness.ConflictLibSource, subdir: "v3");
		var changed = app.Solution.AddMetadataReference(app.ProjectId, MetadataReference.CreateFromFile(v3Path));

		var aligned = changed.WithBaselineReferenceIdentities(baseline, out var pinned);

		aligned.Should().BeSameAs(changed);
		pinned.Should().BeEmpty();
	}

	[TestMethod]
	[Description("A project the baseline never captured (added mid-session) is left alone — EnC skips it anyway.")]
	public void When_ProjectNotInBaseline_Then_Untouched()
	{
		using var app = CreateProject(MetadataReference.CreateFromFile(_v1));
		var baseline = app.Solution.SnapshotReferenceIdentities(out _);

		var late = app.Solution
			.AddProject("Late", "Late", LanguageNames.CSharp)
			.AddMetadataReference(MetadataReference.CreateFromFile(_v2))
			.Solution;

		var aligned = late.WithBaselineReferenceIdentities(baseline, out var pinned);

		aligned.Should().BeSameAs(late);
		pinned.Should().BeEmpty();
	}

	[TestMethod]
	[Description("Assembly simple names match case-insensitively (nuget ids and file systems disagree on casing).")]
	public void When_CasingDiffers_Then_StillPinned()
	{
		var upper = EnCHarness.EmitLibrary(_temp, "CONFLICTLIB", "2.0.0.0", EnCHarness.ConflictLibSource, subdir: "upper");

		using var app = CreateProject(MetadataReference.CreateFromFile(_v1));
		var baseline = app.Solution.SnapshotReferenceIdentities(out _);

		var changed = app.Solution.WithProjectMetadataReferences(
			app.ProjectId,
			[MetadataReference.CreateFromFile(upper)]);

		var aligned = changed.WithBaselineReferenceIdentities(baseline, out var pinned);

		PathsOf(aligned, app.ProjectId).Should().BeEquivalentTo([_v1]);
		pinned.Should().ContainSingle();
	}

	private sealed record AppProject(AdhocWorkspace Workspace, ProjectId ProjectId) : IDisposable
	{
		public Solution Solution => Workspace.CurrentSolution;

		public void Dispose()
			=> Workspace.Dispose();
	}
}

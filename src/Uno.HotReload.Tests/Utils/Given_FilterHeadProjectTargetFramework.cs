using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Tests.Utils;

/// <summary>
/// Tests for <see cref="RoslynExtensions.FilterHeadProjectTargetFramework"/> — the pass
/// restricting a multi-flavor workspace solution to the target framework the running
/// application reported.
/// </summary>
[TestClass]
public sealed class Given_FilterHeadProjectTargetFramework
{
	private static readonly string _headPath = Given_TryGetTargetFramework.ToOsPath("/src/app/app.csproj");

	[TestMethod]
	public void When_TwoFlavors_And_RuntimeMatchesDesktop_Then_AndroidFlavorAndItsClosureAreRemoved()
	{
		using var workspace = new AdhocWorkspace();
		var sharedLib = AddProject(workspace, "lib", filePath: "/src/lib/lib.csproj", refPacks: [NetCorePack()]);
		var androidLib = AddProject(workspace, "lib.droid", filePath: "/src/lib.droid/lib.droid.csproj", refPacks: [AndroidPack()]);
		var androidHead = AddProject(workspace, "app(net10.0-android)", filePath: _headPath, refPacks: [NetCorePack(), AndroidPack()], projectReferences: [sharedLib, androidLib]);
		var desktopHead = AddProject(workspace, "app(net10.0-desktop)", filePath: _headPath, refPacks: [NetCorePack()], defines: ["__DESKTOP__"], projectReferences: [sharedLib]);

		var reporter = new RecordingReporter();
		var filtered = workspace.CurrentSolution.FilterHeadProjectTargetFramework(_headPath, "net10.0-skia", reporter);

		var remainingIds = filtered.ProjectIds.ToHashSet();
		Assert.IsTrue(remainingIds.Contains(desktopHead), "the desktop flavor must be kept");
		Assert.IsTrue(remainingIds.Contains(sharedLib), "a library shared with the kept flavor must be kept");
		Assert.IsFalse(remainingIds.Contains(androidHead), "the android flavor must be removed");
		Assert.IsFalse(remainingIds.Contains(androidLib), "a library only reachable from the removed flavor must be removed");
		Assert.AreEqual(0, reporter.Warnings.Count);
		Assert.AreEqual(0, reporter.Errors.Count);

		// The restriction trace must carry the loaded flavors, the reported TFM and the kept flavors.
		var restriction = reporter.Outputs.Single(message => message.Contains("restricted"));
		StringAssert.Contains(restriction, "restricted to 'net10.0-desktop'");
		StringAssert.Contains(restriction, "'net10.0-skia'");
		StringAssert.Contains(restriction, "net10.0-android");
	}

	[TestMethod]
	public void When_RuntimeTargetFrameworkUnknown_Then_SolutionIsUnchanged_And_Warns()
	{
		using var workspace = new AdhocWorkspace();
		AddProject(workspace, "app(net10.0-android)", filePath: _headPath, refPacks: [AndroidPack()]);
		AddProject(workspace, "app(net10.0-desktop)", filePath: _headPath, refPacks: [NetCorePack()], defines: ["__DESKTOP__"]);

		var reporter = new RecordingReporter();
		var solution = workspace.CurrentSolution;
		var filtered = solution.FilterHeadProjectTargetFramework(_headPath, runtimeTargetFramework: null, reporter);

		Assert.AreSame(solution, filtered);
		Assert.AreEqual(1, reporter.Warnings.Count);
	}

	[TestMethod]
	public void When_NoFlavorMatches_Then_SolutionIsUnchanged_And_Warns()
	{
		using var workspace = new AdhocWorkspace();
		AddProject(workspace, "app(net10.0-android)", filePath: _headPath, refPacks: [AndroidPack()]);
		AddProject(workspace, "app(net10.0-desktop)", filePath: _headPath, refPacks: [NetCorePack()], defines: ["__DESKTOP__"]);

		var reporter = new RecordingReporter();
		var solution = workspace.CurrentSolution;
		var filtered = solution.FilterHeadProjectTargetFramework(_headPath, "net10.0-ios", reporter);

		Assert.AreSame(solution, filtered);
		Assert.AreEqual(1, reporter.Warnings.Count);
	}

	[TestMethod]
	public void When_SingleFlavor_And_RuntimeMatches_Then_SolutionIsUnchanged_And_TracesLoadedVsReported()
	{
		using var workspace = new AdhocWorkspace();
		AddProject(workspace, "app", filePath: _headPath, refPacks: [AndroidPack()]);

		var reporter = new RecordingReporter();
		var solution = workspace.CurrentSolution;

		var filtered = solution.FilterHeadProjectTargetFramework(_headPath, "net10.0-android", reporter);

		Assert.AreSame(solution, filtered);
		Assert.AreEqual(0, reporter.Warnings.Count);
		Assert.AreEqual(0, reporter.Errors.Count);

		var trace = reporter.Outputs.Single();
		StringAssert.Contains(trace, "'net10.0-android36.0'");
		StringAssert.Contains(trace, "'net10.0-android'");
	}

	[TestMethod]
	public void When_SingleFlavor_And_RuntimeDoesNotMatch_Then_SolutionIsUnchanged_And_Warns()
	{
		using var workspace = new AdhocWorkspace();
		AddProject(workspace, "app", filePath: _headPath, refPacks: [AndroidPack()]);

		var reporter = new RecordingReporter();
		var solution = workspace.CurrentSolution;

		// A non-matching runtime TFM must not disturb a single-flavor solution (either the
		// project is single-targeted or MSBuild already pinned the TargetFramework), but a
		// workspace pinned to the wrong flavor must be called out: updates emitted from it
		// will most likely not apply to the running application.
		var filtered = solution.FilterHeadProjectTargetFramework(_headPath, "net10.0-skia", reporter);

		Assert.AreSame(solution, filtered);
		Assert.AreEqual(0, reporter.Errors.Count);

		var warning = reporter.Warnings.Single();
		StringAssert.Contains(warning, "'net10.0-android36.0'");
		StringAssert.Contains(warning, "'net10.0-skia'");
	}

	[TestMethod]
	public void When_SingleFlavor_And_TfmUnresolvable_Then_SolutionIsUnchanged_And_TracesWithoutWarning()
	{
		using var workspace = new AdhocWorkspace();
		// A flavor whose design-time load failed carries no metadata references at all: its TFM
		// cannot be compared to the reported one, so no mismatch warning must be speculated.
		AddProject(workspace, "app", filePath: _headPath, refPacks: []);

		var reporter = new RecordingReporter();
		var solution = workspace.CurrentSolution;

		var filtered = solution.FilterHeadProjectTargetFramework(_headPath, "net10.0-skia", reporter);

		Assert.AreSame(solution, filtered);
		Assert.AreEqual(0, reporter.Warnings.Count);
		Assert.AreEqual(0, reporter.Errors.Count);
		StringAssert.Contains(reporter.Outputs.Single(), "<unresolved");
	}

	[TestMethod]
	public void When_HeadProjectNotFound_Then_SolutionIsUnchanged_And_Errors()
	{
		using var workspace = new AdhocWorkspace();
		AddProject(workspace, "lib", filePath: "/src/lib/lib.csproj", refPacks: [NetCorePack()]);

		var reporter = new RecordingReporter();
		var solution = workspace.CurrentSolution;
		var filtered = solution.FilterHeadProjectTargetFramework(_headPath, "net10.0-skia", reporter);

		Assert.AreSame(solution, filtered);
		Assert.AreEqual(1, reporter.Errors.Count);
	}

	[TestMethod]
	public void When_FlavorTfmUnresolvable_Then_ItIsTreatedAsNonMatching()
	{
		using var workspace = new AdhocWorkspace();
		// A flavor whose design-time load failed carries no metadata references at all.
		var brokenHead = AddProject(workspace, "app(net10.0-ios)", filePath: _headPath, refPacks: []);
		var desktopHead = AddProject(workspace, "app(net10.0-desktop)", filePath: _headPath, refPacks: [NetCorePack()], defines: ["__DESKTOP__"]);

		var reporter = new RecordingReporter();
		var filtered = workspace.CurrentSolution.FilterHeadProjectTargetFramework(_headPath, "net10.0-skia", reporter);

		var remainingIds = filtered.ProjectIds.ToHashSet();
		Assert.IsTrue(remainingIds.Contains(desktopHead));
		Assert.IsFalse(remainingIds.Contains(brokenHead));
	}

	[TestMethod]
	public void When_RuntimeIsMobile_Then_MobileFlavorIsKept()
	{
		using var workspace = new AdhocWorkspace();
		var androidHead = AddProject(workspace, "app(net10.0-android)", filePath: _headPath, refPacks: [NetCorePack(), AndroidPack()]);
		var desktopHead = AddProject(workspace, "app(net10.0-desktop)", filePath: _headPath, refPacks: [NetCorePack()], defines: ["__DESKTOP__"]);

		var reporter = new RecordingReporter();
		var filtered = workspace.CurrentSolution.FilterHeadProjectTargetFramework(_headPath, "net10.0-android", reporter);

		var remainingIds = filtered.ProjectIds.ToHashSet();
		Assert.IsTrue(remainingIds.Contains(androidHead), "the android flavor must be kept (platform-version-tolerant match)");
		Assert.IsFalse(remainingIds.Contains(desktopHead), "the desktop flavor must be removed");
	}

	// ────────────────────────────────────────────────────────────────────────
	// Fixture
	// ────────────────────────────────────────────────────────────────────────

	private static string NetCorePack()
		=> Given_TryGetTargetFramework.FakeRefPackPath("Microsoft.NETCore.App.Ref", "10.0.4", "net10.0", "System.Runtime.dll");

	private static string AndroidPack()
		=> Given_TryGetTargetFramework.FakeRefPackPath("Microsoft.Android.Ref.net10.0_36.0", "36.0.42", "net10.0", "Microsoft.Android.dll");

	private static ProjectId AddProject(
		AdhocWorkspace workspace,
		string name,
		string filePath,
		IReadOnlyList<string> refPacks,
		IReadOnlyList<string>? defines = null,
		IReadOnlyList<ProjectId>? projectReferences = null)
	{
		var projectId = ProjectId.CreateNewId(name);

		var parseOptions = defines is { Count: > 0 }
			? new CSharpParseOptions().WithPreprocessorSymbols(defines)
			: new CSharpParseOptions();

		var projectInfo = ProjectInfo.Create(
			projectId,
			VersionStamp.Default,
			name: name,
			assemblyName: name,
			language: LanguageNames.CSharp,
			filePath: Given_TryGetTargetFramework.ToOsPath(filePath),
			parseOptions: parseOptions,
			metadataReferences: refPacks.Select(Given_TryGetTargetFramework.StubMetadataReference.Create).ToImmutableArray<MetadataReference>(),
			projectReferences: projectReferences?.Select(id => new ProjectReference(id)) ?? []);

		workspace.AddProject(projectInfo);
		return projectId;
	}
}

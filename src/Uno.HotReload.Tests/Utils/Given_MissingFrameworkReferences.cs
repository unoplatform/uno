using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Utils;
using static Uno.HotReload.Tests.Utils.Given_TryGetTargetFramework;

namespace Uno.HotReload.Tests.Utils;

/// <summary>
/// Tests for the missing-targeting-pack detection helpers (spec 049):
/// <see cref="RoslynExtensions.HasFrameworkReferences"/>,
/// <see cref="RoslynExtensions.IsMissingFrameworkReferences"/> and
/// <see cref="RoslynExtensions.TryGetDotnetRootFromFrameworkReferences"/>.
/// </summary>
[TestClass]
public sealed class Given_MissingFrameworkReferences
{
	[TestMethod]
	public void HasFrameworkReferences_SdkInstalledRefPack_True()
	{
		var project = CreateProject(
			refPaths: [ToOsPath("/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.7/ref/net10.0/System.Runtime.dll")],
			preprocessorSymbols: []);

		Assert.IsTrue(project.HasFrameworkReferences());
		Assert.IsFalse(project.IsMissingFrameworkReferences());
	}

	[TestMethod]
	public void HasFrameworkReferences_NugetCachedRefPack_True()
	{
		// A restored PackageDownload lands in the NuGet cache: still a framework reference.
		var project = CreateProject(
			refPaths: [ToOsPath("/home/user/.nuget/packages/microsoft.netcore.app.ref/10.0.5/ref/net10.0/System.Runtime.dll")],
			preprocessorSymbols: []);

		Assert.IsTrue(project.HasFrameworkReferences());
		Assert.IsFalse(project.IsMissingFrameworkReferences());
	}

	[TestMethod]
	public void IsMissingFrameworkReferences_NugetLibsOnly_True()
	{
		// The field signature: the design-time build resolved NuGet package references but no
		// targeting pack (missing on disk, PackageDownload never materialized — no restore).
		var project = CreateProject(
			refPaths:
			[
				ToOsPath("/home/user/.nuget/packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll"),
				ToOsPath("/home/user/.nuget/packages/skiasharp/3.119.2/ref/net8.0/SkiaSharp.dll"),
			],
			preprocessorSymbols: []);

		Assert.IsFalse(project.HasFrameworkReferences());
		Assert.IsTrue(project.IsMissingFrameworkReferences());
	}

	[TestMethod]
	public void IsMissingFrameworkReferences_NoReferencesAtAll_False()
	{
		// No references at all is the design-time-build-failed state, not the
		// missing-targeting-pack one — Roslyn reports that through WorkspaceFailed already.
		var project = CreateProject(refPaths: [], preprocessorSymbols: []);

		Assert.IsFalse(project.HasFrameworkReferences());
		Assert.IsFalse(project.IsMissingFrameworkReferences());
	}

	[TestMethod]
	public void TryGetDotnetRoot_SdkInstalledRefPack_ReturnsRoot()
	{
		var project = CreateProject(
			refPaths: [ToOsPath("/home/user/.dotnet/packs/Microsoft.NETCore.App.Ref/10.0.7/ref/net10.0/System.Runtime.dll")],
			preprocessorSymbols: []);

		Assert.IsTrue(project.TryGetDotnetRootFromFrameworkReferences(out var root));
		Assert.AreEqual(ToOsPath("/home/user/.dotnet"), root);
	}

	[TestMethod]
	public void TryGetDotnetRoot_NugetCachedRefPack_ReturnsFalse()
	{
		// The NuGet-cache layout carries no SDK root ("packages", not "packs").
		var project = CreateProject(
			refPaths: [ToOsPath("/home/user/.nuget/packages/microsoft.netcore.app.ref/10.0.5/ref/net10.0/System.Runtime.dll")],
			preprocessorSymbols: []);

		Assert.IsFalse(project.TryGetDotnetRootFromFrameworkReferences(out var root));
		Assert.IsNull(root);
	}

	[TestMethod]
	public void TryGetDotnetRoot_MixedReferences_SkipsToSdkInstalledPack()
	{
		var project = CreateProject(
			refPaths:
			[
				ToOsPath("/home/user/.nuget/packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll"),
				ToOsPath("/home/user/.nuget/packages/microsoft.netcore.app.ref/10.0.5/ref/net10.0/System.Runtime.dll"),
				ToOsPath("/usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.7/ref/net10.0/System.Linq.dll"),
			],
			preprocessorSymbols: []);

		Assert.IsTrue(project.TryGetDotnetRootFromFrameworkReferences(out var root));
		Assert.AreEqual(ToOsPath("/usr/lib/dotnet"), root);
	}

	[TestMethod]
	public void GetHeadFlavorsMissingFrameworkReferences_MixedSolution_ReturnsOnlyBrokenHeadFlavors()
	{
		var headPath = ToOsPath("/src/app/app.csproj");
		var libPath = ToOsPath("/src/lib/lib.csproj");

		using var workspace = new AdhocWorkspace();
		var solution = workspace.CurrentSolution
			// Healthy head flavor (desktop): SDK-installed ref pack.
			.AddProject(CreateProjectInfo("app(net10.0-desktop)", headPath,
				[ToOsPath("/home/user/.dotnet/packs/Microsoft.NETCore.App.Ref/10.0.7/ref/net10.0/System.Runtime.dll")]))
			// Broken head flavor (wasm): NuGet libs only.
			.AddProject(CreateProjectInfo("app(net10.0-browserwasm)", headPath,
				[ToOsPath("/home/user/.nuget/packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll")]))
			// Broken-looking *library* flavor: must not be reported (head-only scan).
			.AddProject(CreateProjectInfo("lib(net10.0-browserwasm)", libPath,
				[ToOsPath("/home/user/.nuget/packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll")]));

		var broken = solution.GetHeadFlavorsMissingFrameworkReferences(headPath);

		Assert.AreEqual(1, broken.Count);
		Assert.AreEqual("app(net10.0-browserwasm)", broken[0].Name);
	}

	private static ProjectInfo CreateProjectInfo(string name, string filePath, IReadOnlyList<string> refPaths)
		=> ProjectInfo.Create(
			ProjectId.CreateNewId(),
			VersionStamp.Default,
			name: name,
			assemblyName: name,
			language: LanguageNames.CSharp,
			filePath: filePath,
			metadataReferences: refPaths.Select(StubMetadataReference.Create).ToImmutableArray<MetadataReference>());
}

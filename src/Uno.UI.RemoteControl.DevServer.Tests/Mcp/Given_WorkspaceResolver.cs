using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

[TestClass]
public class Given_WorkspaceResolver
{
	[TestMethod]
	public async Task WhenSingleValidWorkspaceIsNested_AutoDescendsToWorkspace()
	{
		var root = CreateTempDirectory();
		try
		{
			var src = Path.Combine(root, "src");
			Directory.CreateDirectory(src);
			await File.WriteAllTextAsync(Path.Combine(src, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(src, "StudioLive.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeTrue();
			result.EffectiveWorkspaceDirectory.Should().Be(src);
			result.SelectedSolutionPath.Should().Be(Path.Combine(src, "StudioLive.slnx"));
			result.SelectedGlobalJsonPath.Should().Be(Path.Combine(src, "global.json"));
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.AutoDiscovered);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenSingleValidWorkspaceUsesSln_AutoDescendsToWorkspace()
	{
		var root = CreateTempDirectory();
		try
		{
			var src = Path.Combine(root, "src");
			Directory.CreateDirectory(src);
			await File.WriteAllTextAsync(Path.Combine(src, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(src, "StudioLive.sln"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeTrue();
			result.SelectedSolutionPath.Should().Be(Path.Combine(src, "StudioLive.sln"));
			result.SelectedGlobalJsonPath.Should().Be(Path.Combine(src, "global.json"));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenDirectoryIsEmpty_ResultIsNoCandidates()
	{
		var root = CreateTempDirectory();
		try
		{
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeFalse();
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.NoCandidates);
			result.CandidateSolutions.Should().BeEmpty();
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenGitRepoHasNoSolutions_ResultIsNoCandidates()
	{
		var root = CreateTempDirectory();
		try
		{
			Directory.CreateDirectory(Path.Combine(root, ".git"));
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeFalse();
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.NoCandidates);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenRequestedDirectoryIsValidUnoWorkspace_UsesCurrentDirectory()
	{
		var root = CreateTempDirectory();
		try
		{
			await File.WriteAllTextAsync(Path.Combine(root, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(root, "StudioLive.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeTrue();
			result.EffectiveWorkspaceDirectory.Should().Be(root);
			result.SelectedSolutionPath.Should().Be(Path.Combine(root, "StudioLive.slnx"));
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.CurrentDirectory);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenMultipleCandidatesOnlyOneHasValidUnoGlobalJson_SelectsThatWorkspace()
	{
		var root = CreateTempDirectory();
		try
		{
			var srcA = Path.Combine(root, "srcA");
			var srcB = Path.Combine(root, "srcB");
			Directory.CreateDirectory(srcA);
			Directory.CreateDirectory(srcB);

			await File.WriteAllTextAsync(Path.Combine(srcA, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(srcA, "AppA.slnx"), string.Empty);
			await File.WriteAllTextAsync(Path.Combine(srcB, "global.json"), """{"sdk":{"version":"10.0.100"}}""");
			await File.WriteAllTextAsync(Path.Combine(srcB, "AppB.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeTrue();
			result.EffectiveWorkspaceDirectory.Should().Be(srcA);
			result.SelectedSolutionPath.Should().Be(Path.Combine(srcA, "AppA.slnx"));
			result.CandidateSolutions.Should().BeEquivalentTo(
			[
				Path.Combine(srcA, "AppA.slnx"),
				Path.Combine(srcB, "AppB.slnx"),
			]);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenRootSolutionIsNonUnoButNestedSolutionIsUno_SelectsNestedUnoWorkspace()
	{
		var root = CreateTempDirectory();
		try
		{
			await File.WriteAllTextAsync(Path.Combine(root, "global.json"), """{"sdk":{"version":"10.0.100"}}""");
			await File.WriteAllTextAsync(Path.Combine(root, "RootApp.slnx"), string.Empty);

			var src = Path.Combine(root, "src");
			Directory.CreateDirectory(src);
			await File.WriteAllTextAsync(Path.Combine(src, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(src, "UnoApp.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeTrue();
			result.EffectiveWorkspaceDirectory.Should().Be(src);
			result.SelectedSolutionPath.Should().Be(Path.Combine(src, "UnoApp.slnx"));
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.AutoDiscovered);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenMultipleUnoCandidatesHaveDifferentDistance_SelectsClosestWorkspace()
	{
		var root = CreateTempDirectory();
		try
		{
			var appA = Path.Combine(root, "groupA", "appA");
			var appB = Path.Combine(root, "appB");
			var appBNested = Path.Combine(appB, "nested");
			Directory.CreateDirectory(appA);
			Directory.CreateDirectory(appBNested);

			await File.WriteAllTextAsync(Path.Combine(root, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.root"}}""");
			await File.WriteAllTextAsync(Path.Combine(appA, "AppA.slnx"), string.Empty);
			await File.WriteAllTextAsync(Path.Combine(appB, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.appB"}}""");
			await File.WriteAllTextAsync(Path.Combine(appBNested, "AppB.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeTrue();
			result.EffectiveWorkspaceDirectory.Should().Be(appB);
			result.SelectedSolutionPath.Should().Be(Path.Combine(appBNested, "AppB.slnx"));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenMultipleCandidatesHaveSamePriority_ResultIsAmbiguous()
	{
		var root = CreateTempDirectory();
		try
		{
			var srcA = Path.Combine(root, "srcA");
			var srcB = Path.Combine(root, "srcB");
			Directory.CreateDirectory(srcA);
			Directory.CreateDirectory(srcB);

			await File.WriteAllTextAsync(Path.Combine(srcA, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(srcA, "AppA.slnx"), string.Empty);
			await File.WriteAllTextAsync(Path.Combine(srcB, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(srcB, "AppB.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeFalse();
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.Ambiguous);
			result.CandidateSolutions.Should().HaveCount(2);
			result.SelectedSolutionPath.Should().BeNull();
			result.EffectiveWorkspaceDirectory.Should().BeNull();
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenSolutionHasNoGlobalJsonOnParentChain_ResultIsNoValidWorkspace()
	{
		var root = CreateTempDirectory();
		try
		{
			await File.WriteAllTextAsync(Path.Combine(root, "App.slnx"), string.Empty);
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeFalse();
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.NoValidWorkspace);
			result.CandidateSolutions.Should().ContainSingle().Which.Should().Be(Path.Combine(root, "App.slnx"));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenSolutionsExistButNoUnoWorkspace_ResultIsNoValidWorkspace()
	{
		var root = CreateTempDirectory();
		try
		{
			var src = Path.Combine(root, "src");
			Directory.CreateDirectory(src);
			await File.WriteAllTextAsync(Path.Combine(src, "global.json"), """{"sdk":{"version":"10.0.100"}}""");
			await File.WriteAllTextAsync(Path.Combine(src, "App.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeFalse();
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.NoValidWorkspace);
			result.CandidateSolutions.Should().Contain(Path.Combine(src, "App.slnx"));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenGlobalJsonIsMalformed_ResultIsNoValidWorkspace()
	{
		var root = CreateTempDirectory();
		try
		{
			await File.WriteAllTextAsync(Path.Combine(root, "global.json"), "{ not valid json");
			await File.WriteAllTextAsync(Path.Combine(root, "App.slnx"), string.Empty);
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeFalse();
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.NoValidWorkspace);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenNearestGlobalJsonIsNonUno_ParentUnoGlobalJsonIsIgnored()
	{
		var root = CreateTempDirectory();
		try
		{
			var src = Path.Combine(root, "src");
			Directory.CreateDirectory(src);
			await File.WriteAllTextAsync(Path.Combine(root, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.parent"}}""");
			await File.WriteAllTextAsync(Path.Combine(src, "global.json"), """{"sdk":{"version":"10.0.100"}}""");
			await File.WriteAllTextAsync(Path.Combine(src, "App.slnx"), string.Empty);
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeFalse();
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.NoValidWorkspace);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenNearestGlobalJsonIsUno_UsesNearestWorkspace()
	{
		var root = CreateTempDirectory();
		try
		{
			var src = Path.Combine(root, "src");
			Directory.CreateDirectory(src);
			await File.WriteAllTextAsync(Path.Combine(root, "global.json"), """{"sdk":{"version":"10.0.100"}}""");
			await File.WriteAllTextAsync(Path.Combine(src, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.nearest"}}""");
			await File.WriteAllTextAsync(Path.Combine(src, "App.slnx"), string.Empty);
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeTrue();
			result.EffectiveWorkspaceDirectory.Should().Be(src);
			result.SelectedGlobalJsonPath.Should().Be(Path.Combine(src, "global.json"));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task WhenSolutionIsDeeperThanThreeLevels_ResultIsNoCandidates()
	{
		var root = CreateTempDirectory();
		try
		{
			var deepDirectory = Path.Combine(root, "a", "b", "c", "d");
			Directory.CreateDirectory(deepDirectory);
			await File.WriteAllTextAsync(Path.Combine(deepDirectory, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(deepDirectory, "DeepApp.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var result = await resolver.ResolveAsync(root);

			result.IsResolved.Should().BeFalse();
			result.ResolutionKind.Should().Be(WorkspaceResolutionKind.NoCandidates);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public void WorkspaceHash_UsesEffectiveWorkspaceDirectory()
	{
		var rootHash = ToolCacheFile.ComputeWorkspaceHash(@"D:\src\studio.live");
		var workspaceHash = ToolCacheFile.ComputeWorkspaceHash(@"D:\src\studio.live\src");

		workspaceHash.Should().NotBe(rootHash);
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"uno-workspace-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}
}

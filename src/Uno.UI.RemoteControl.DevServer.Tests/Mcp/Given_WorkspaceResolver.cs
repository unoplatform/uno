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

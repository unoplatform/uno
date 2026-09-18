using AwesomeAssertions;
using Uno.UI.Tasks.RuntimeAssetsSelector;

namespace Uno.UI.Tasks.Tests;

[TestClass]
public class Given_RuntimeAssetsSelectorTask
{
	private static RuntimeAssetsSelectorTask_v0 CreateTask(PackageCacheFixture fixture, string platformAsset, string winRTRuntimeIdentifier)
		=> new()
		{
			BuildEngine = new RecordingBuildEngine(),
			UnoRuntimeEnabledPackage = [],
			UnoRuntimeIdentifier = "",
			UnoUIRuntimeIdentifier = "skia",
			UnoWinRTRuntimeIdentifier = winRTRuntimeIdentifier,
			TargetFrameworkVersion = "v10.0",
			ResolvedCompileFileDefinitionsInput =
			[
				PackageCacheFixture.Item(platformAsset, ("NuGetPackageId", "Sample.Lib"), ("NuGetPackageVersion", "1.0.0")),
			],
			RuntimeCopyLocalItemsInput =
			[
				PackageCacheFixture.Item(platformAsset, ("NuGetPackageId", "Sample.Lib")),
			],
		};

	[TestMethod]
	[DataRow("android", "net10.0-android35.0")]
	[DataRow("ios", "net10.0-ios26.0")]
	[DataRow("tvos", "net10.0-tvos26.0")]
	public void When_PlatformSpecificAsset_Then_It_Is_Kept(string winRTRuntimeIdentifier, string platformTargetFramework)
	{
		using var fixture = new PackageCacheFixture(nameof(When_PlatformSpecificAsset_Then_It_Is_Kept));
		var platformAsset = fixture.AddPackage("Sample.Lib", "1.0.0", platformTargetFramework, "net10.0", ["Microsoft.UI.Xaml.UIElement"]);

		var task = CreateTask(fixture, platformAsset, winRTRuntimeIdentifier);

		task.Execute().Should().BeTrue();

		task.ResolvedCompileFileDefinitionsToRemove.Should().BeEmpty();
		task.ResolvedCompileFileDefinitionsToAdd.Should().BeEmpty();
		task.RuntimeCopyLocalItemsToRemove.Should().BeEmpty();
		task.RuntimeCopyLocalItemsToAdd.Should().BeEmpty();
	}

	[TestMethod]
	public void When_WebAssembly_Then_Assets_Are_Untouched()
	{
		using var fixture = new PackageCacheFixture(nameof(When_WebAssembly_Then_Assets_Are_Untouched));
		var platformAsset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", ["Microsoft.UI.Xaml.UIElement"]);

		var task = CreateTask(fixture, platformAsset, "webassembly");

		task.Execute().Should().BeTrue();

		task.ResolvedCompileFileDefinitionsToRemove.Should().BeEmpty();
		task.ResolvedCompileFileDefinitionsToAdd.Should().BeEmpty();
		task.RuntimeCopyLocalItemsToRemove.Should().BeEmpty();
		task.RuntimeCopyLocalItemsToAdd.Should().BeEmpty();
	}

	[TestMethod]
	public void When_SingleLayer_Then_Task_Is_A_NoOp()
	{
		using var fixture = new PackageCacheFixture(nameof(When_SingleLayer_Then_Task_Is_A_NoOp));
		var platformAsset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", ["Microsoft.UI.Xaml.UIElement"]);

		var task = CreateTask(fixture, platformAsset, winRTRuntimeIdentifier: "");
		task.UnoRuntimeIdentifier = "skia";
		task.UnoUIRuntimeIdentifier = "";

		task.Execute().Should().BeTrue();

		task.ResolvedCompileFileDefinitionsToRemove.Should().BeEmpty();
		task.ResolvedCompileFileDefinitionsToAdd.Should().BeEmpty();
		task.RuntimeCopyLocalItemsToRemove.Should().BeEmpty();
		task.RuntimeCopyLocalItemsToAdd.Should().BeEmpty();
	}
}

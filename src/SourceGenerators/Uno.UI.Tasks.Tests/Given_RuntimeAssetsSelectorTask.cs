using AwesomeAssertions;
using Uno.UI.Tasks.RuntimeAssetsSelector;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Covers the non-runtime-enabled half of the selection: a multi-targeted library's platform-specific asset is
/// the one that ships, and nothing rewrites it. Every case drives a real runtime-enabled package alongside it,
/// otherwise the task returns before reaching any of the behaviour these tests claim to pin.
/// </summary>
[TestClass]
public class Given_RuntimeAssetsSelectorTask
{
	private const string NeutralTargetFramework = "net10.0";

	private static RuntimeAssetsSelectorTask_v0 CreateTask(
		PackageCacheFixture fixture,
		string platformAsset,
		string winRTRuntimeIdentifier,
		string runtimeEnabledPlatformTargetFramework = "net10.0-android30.0")
	{
		// Without a runtime-enabled package the task has nothing to iterate and no assertion below can fail.
		var packageBasePath = fixture.AddRuntimeEnabledPackage(
			"Uno.WinRT",
			"1.0.0",
			NeutralTargetFramework,
			runtimeEnabledPlatformTargetFramework,
			["Uno.WinRT"],
			[],
			["skia", "webassembly"]);

		return new()
		{
			BuildEngine = new RecordingBuildEngine(),
			UnoRuntimeEnabledPackage = [PackageCacheFixture.Item("Uno.WinRT", ("PackageBasePath", packageBasePath))],
			UnoRuntimeIdentifier = "",
			UnoUIRuntimeIdentifier = "skia",
			UnoWinRTRuntimeIdentifier = winRTRuntimeIdentifier,
			TargetFrameworkVersion = "v10.0",
			ResolvedCompileFileDefinitionsInput =
			[
				PackageCacheFixture.Item(platformAsset, ("NuGetPackageId", "Sample.Lib"), ("NuGetPackageVersion", "1.0.0")),
				PackageCacheFixture.Item(
					Path.Combine(fixture.Root, "Uno.WinRT", "1.0.0", "lib", NeutralTargetFramework, "Uno.WinRT.dll"),
					("NuGetPackageId", "Uno.WinRT"), ("NuGetPackageVersion", "1.0.0")),
			],
			RuntimeCopyLocalItemsInput =
			[
				PackageCacheFixture.Item(platformAsset, ("NuGetPackageId", "Sample.Lib")),
				PackageCacheFixture.Item(
					Path.Combine(fixture.Root, "Uno.WinRT", "1.0.0", "lib", NeutralTargetFramework, "Uno.WinRT.dll"),
					("NuGetPackageId", "Uno.WinRT")),
			],
		};
	}

	private static IEnumerable<string> SampleLibPaths(Microsoft.Build.Framework.ITaskItem[]? items)
		=> (items ?? [])
			.Select(item => item.ItemSpec.Replace('\\', '/'))
			.Where(path => path.Contains("Sample.Lib", StringComparison.Ordinal));

	[TestMethod]
	[DataRow("android", "net10.0-android35.0")]
	[DataRow("ios", "net10.0-ios26.0")]
	[DataRow("tvos", "net10.0-tvos26.0")]
	public void When_PlatformSpecificAsset_Then_It_Is_Kept(string winRTRuntimeIdentifier, string platformTargetFramework)
	{
		using var fixture = new PackageCacheFixture(nameof(When_PlatformSpecificAsset_Then_It_Is_Kept));
		var platformAsset = fixture.AddPackage("Sample.Lib", "1.0.0", platformTargetFramework, NeutralTargetFramework, ["Microsoft.UI.Xaml.UIElement"]);

		// The runtime-enabled package must carry the head's platform implementation, as the shipped one does.
		var task = CreateTask(fixture, platformAsset, winRTRuntimeIdentifier, platformTargetFramework);

		task.Execute().Should().BeTrue();

		SampleLibPaths(task.ResolvedCompileFileDefinitionsToRemove).Should().BeEmpty();
		SampleLibPaths(task.ResolvedCompileFileDefinitionsToAdd).Should().BeEmpty();
		SampleLibPaths(task.RuntimeCopyLocalItemsToRemove).Should().BeEmpty();
		SampleLibPaths(task.RuntimeCopyLocalItemsToAdd).Should().BeEmpty();
	}

	[TestMethod]
	public void When_WebAssembly_Then_Assets_Are_Untouched()
	{
		using var fixture = new PackageCacheFixture(nameof(When_WebAssembly_Then_Assets_Are_Untouched));
		var platformAsset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", NeutralTargetFramework, ["Microsoft.UI.Xaml.UIElement"]);

		var task = CreateTask(fixture, platformAsset, "webassembly");

		task.Execute().Should().BeTrue();

		SampleLibPaths(task.ResolvedCompileFileDefinitionsToRemove).Should().BeEmpty();
		SampleLibPaths(task.RuntimeCopyLocalItemsToRemove).Should().BeEmpty();
	}

	[TestMethod]
	public void When_SingleLayer_Then_Plain_Assets_Are_Untouched()
	{
		using var fixture = new PackageCacheFixture(nameof(When_SingleLayer_Then_Plain_Assets_Are_Untouched));
		var platformAsset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", NeutralTargetFramework, ["Microsoft.UI.Xaml.UIElement"]);

		var task = CreateTask(fixture, platformAsset, winRTRuntimeIdentifier: "");
		task.UnoRuntimeIdentifier = "skia";
		task.UnoUIRuntimeIdentifier = "";

		task.Execute().Should().BeTrue();

		SampleLibPaths(task.ResolvedCompileFileDefinitionsToRemove).Should().BeEmpty();
		SampleLibPaths(task.RuntimeCopyLocalItemsToRemove).Should().BeEmpty();

		// The runtime-enabled package still resolves, proving the loop ran.
		(task.RuntimeCopyLocalItemsToAdd ?? []).Should().NotBeEmpty();
	}

	[TestMethod]
	public void When_No_Identifier_Is_Set_Then_Nothing_Is_Rewritten()
	{
		using var fixture = new PackageCacheFixture(nameof(When_No_Identifier_Is_Set_Then_Nothing_Is_Rewritten));
		var platformAsset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", NeutralTargetFramework, []);

		var task = CreateTask(fixture, platformAsset, winRTRuntimeIdentifier: "");
		task.UnoUIRuntimeIdentifier = "";

		// A plain netX.0 project: no runtime host, so the task must stay out of the way without complaining.
		task.Execute().Should().BeTrue();
		((RecordingBuildEngine)task.BuildEngine).Errors.Should().BeEmpty();
	}

	[TestMethod]
	public void When_WinRT_Layer_Is_Unsupported_Then_It_Is_An_Error()
	{
		using var fixture = new PackageCacheFixture(nameof(When_WinRT_Layer_Is_Unsupported_Then_It_Is_An_Error));
		var platformAsset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", NeutralTargetFramework, []);

		// A two-layer head whose WinRT layer names a platform nothing can serve.
		var task = CreateTask(fixture, platformAsset, winRTRuntimeIdentifier: "maccatalyst");

		task.Execute().Should().BeFalse();
		((RecordingBuildEngine)task.BuildEngine).Errors.Should().NotBeEmpty();
	}

	[TestMethod]
	public void When_Mode_Is_Half_Configured_Then_It_Is_An_Error()
	{
		using var fixture = new PackageCacheFixture(nameof(When_Mode_Is_Half_Configured_Then_It_Is_An_Error));
		var platformAsset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", NeutralTargetFramework, []);

		// A WinRT layer without a UI layer matches neither mode. Returning silently here leaves every
		// runtime-enabled package on its reference facade in an otherwise green build.
		var task = CreateTask(fixture, platformAsset, winRTRuntimeIdentifier: "android");
		task.UnoUIRuntimeIdentifier = "";

		task.Execute().Should().BeFalse();
		((RecordingBuildEngine)task.BuildEngine).Errors.Should().NotBeEmpty();
	}
}

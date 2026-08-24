using AwesomeAssertions;
using Microsoft.Build.Framework;
using Uno.UI.Tasks.RuntimeAssetsSelector;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Covers the selection Uno.WinRT and Uno.Foundation rely on: the assemblies come from the shared runtime
/// folder while the WinRT ones are redirected to the implementation matching the head's platform. This is what
/// lets a library built for a plain netX.0 call a WinRT API and still reach the platform implementation.
/// </summary>
[TestClass]
public class Given_RuntimeEnabledPackage
{
	private const string NeutralTargetFramework = "net10.0";
	private const string AndroidTargetFramework = "net10.0-android30.0";

	/// <summary>
	/// The target platform and the lib folder it must resolve to.
	/// </summary>
	private static readonly (string Platform, string TargetFramework)[] MobilePlatforms =
	[
		("android", "net10.0-android30.0"),
		("ios", "net10.0-ios18.0"),
		("tvos", "net10.0-tvos18.0"),
	];

	private static readonly string[] WinRTAssemblies = ["Uno.WinRT", "Uno.Foundation", "Uno.UI.Dispatching"];
	private static readonly string[] OtherAssemblies = ["Contoso.CrossRuntime"];

	private static (RuntimeAssetsSelectorTask_v0 Task, string PackageBasePath) CreateTask(
		PackageCacheFixture fixture,
		string targetPlatformIdentifier,
		string platformTargetFramework = AndroidTargetFramework)
	{
		var packageBasePath = fixture.AddRuntimeEnabledPackage(
			"Uno.WinRT",
			"1.0.0",
			NeutralTargetFramework,
			platformTargetFramework,
			WinRTAssemblies,
			OtherAssemblies,
			["skia", "webassembly"]);

		// A plain netX.0 library: it only ships a platform-neutral asset and is not runtime-enabled.
		var plainLibrary = fixture.AddPackage("Contoso.Sensors", "2.0.0", "net10.0-android35.0", NeutralTargetFramework, []);
		var plainNeutralAsset = Path.Combine(fixture.Root, "Contoso.Sensors", "2.0.0", "lib", NeutralTargetFramework, "Contoso.Sensors.dll");

		var task = new RuntimeAssetsSelectorTask_v0
		{
			BuildEngine = new RecordingBuildEngine(),
			UnoRuntimeEnabledPackage = [PackageCacheFixture.Item("Uno.WinRT", ("PackageBasePath", packageBasePath))],
			TargetPlatformIdentifier = targetPlatformIdentifier,
			TargetFrameworkVersion = "v10.0",
			ResolvedCompileFileDefinitionsInput =
			[
				PackageCacheFixture.Item(
					Path.Combine(fixture.Root, "Uno.WinRT", "1.0.0", "lib", NeutralTargetFramework, "Uno.WinRT.dll"),
					("NuGetPackageId", "Uno.WinRT"), ("NuGetPackageVersion", "1.0.0")),
				PackageCacheFixture.Item(plainNeutralAsset, ("NuGetPackageId", "Contoso.Sensors")),
			],
			RuntimeCopyLocalItemsInput =
			[
				PackageCacheFixture.Item(
					Path.Combine(fixture.Root, "Uno.WinRT", "1.0.0", "lib", NeutralTargetFramework, "Uno.WinRT.dll"),
					("NuGetPackageId", "Uno.WinRT")),
				PackageCacheFixture.Item(plainNeutralAsset, ("NuGetPackageId", "Contoso.Sensors")),
			],
		};

		_ = plainLibrary;
		return (task, packageBasePath);
	}

	private static IEnumerable<string> Paths(ITaskItem[]? items)
		=> (items ?? []).Select(item => item.ItemSpec.Replace('\\', '/'));

	[TestMethod]
	[DynamicData(nameof(MobilePlatformData))]
	public void When_MobileHead_Then_WinRT_Assemblies_Come_From_The_Platform_Implementation(string platform, string platformTargetFramework)
	{
		using var fixture = new PackageCacheFixture($"{nameof(When_MobileHead_Then_WinRT_Assemblies_Come_From_The_Platform_Implementation)}_{platform}");
		var (task, _) = CreateTask(
			fixture,
			targetPlatformIdentifier: platform,
			platformTargetFramework: platformTargetFramework);

		task.Execute().Should().BeTrue();

		var added = Paths(task.RuntimeCopyLocalItemsToAdd).ToList();

		foreach (var winRTAssembly in WinRTAssemblies)
		{
			added.Should().Contain(
				path => path.EndsWith($"lib/{platformTargetFramework}/{winRTAssembly}.dll", StringComparison.Ordinal),
				$"{winRTAssembly} must resolve to the {platform} implementation");
		}

		// Everything else stays on the shared build.
		added.Should().Contain(path => path.EndsWith($"uno-runtime/{NeutralTargetFramework}/skia/Contoso.CrossRuntime.dll", StringComparison.Ordinal));
		added.Should().NotContain(path => path.EndsWith($"uno-runtime/{NeutralTargetFramework}/skia/Uno.WinRT.dll", StringComparison.Ordinal));
		added.Should().NotContain(path => path.Contains("/webassembly/", StringComparison.Ordinal));
	}

	public static IEnumerable<object[]> MobilePlatformData
		=> MobilePlatforms.Select(platform => new object[] { platform.Platform, platform.TargetFramework });

	[TestMethod]
	public void When_WebAssemblyHead_Then_Compile_References_Are_Not_Rewritten()
	{
		using var fixture = new PackageCacheFixture(nameof(When_WebAssemblyHead_Then_Compile_References_Are_Not_Rewritten));
		var (task, _) = CreateTask(fixture, targetPlatformIdentifier: "browserwasm");

		task.Execute().Should().BeTrue();

		// The mobile heads swap compile references so a WinRT call binds the platform implementation; the browser
		// head deliberately does not. This asymmetry is the only behaviour the two-layer split still carries.
		task.ResolvedCompileFileDefinitionsToAdd.Should().BeEmpty();
		task.ResolvedCompileFileDefinitionsToRemove.Should().BeEmpty();
	}

	[TestMethod]
	public void When_WebAssemblyHead_Then_WinRT_Assemblies_Come_From_The_WebAssembly_Runtime()
	{
		using var fixture = new PackageCacheFixture(nameof(When_WebAssemblyHead_Then_WinRT_Assemblies_Come_From_The_WebAssembly_Runtime));
		var (task, _) = CreateTask(fixture, targetPlatformIdentifier: "browserwasm");

		task.Execute().Should().BeTrue();

		var added = Paths(task.RuntimeCopyLocalItemsToAdd).ToList();

		foreach (var winRTAssembly in WinRTAssemblies)
		{
			added.Should().Contain(
				path => path.EndsWith($"uno-runtime/{NeutralTargetFramework}/webassembly/{winRTAssembly}.dll", StringComparison.Ordinal));
		}

		added.Should().Contain(path => path.EndsWith($"uno-runtime/{NeutralTargetFramework}/skia/Contoso.CrossRuntime.dll", StringComparison.Ordinal));
	}

	[TestMethod]
	public void When_DesktopHead_Then_Everything_Comes_From_The_Skia_Runtime()
	{
		using var fixture = new PackageCacheFixture(nameof(When_DesktopHead_Then_Everything_Comes_From_The_Skia_Runtime));
		var (task, _) = CreateTask(fixture, targetPlatformIdentifier: "desktop");

		task.Execute().Should().BeTrue();

		var added = Paths(task.RuntimeCopyLocalItemsToAdd).ToList();

		foreach (var assembly in WinRTAssemblies.Concat(OtherAssemblies))
		{
			added.Should().Contain(
				path => path.EndsWith($"uno-runtime/{NeutralTargetFramework}/skia/{assembly}.dll", StringComparison.Ordinal));
		}

		// Desktop heads keep compiling against the platform-neutral surface.
		task.ResolvedCompileFileDefinitionsToAdd.Should().BeEmpty();
		task.ResolvedCompileFileDefinitionsToRemove.Should().BeEmpty();
	}

	[TestMethod]
	public void When_HeadlessHead_Then_Everything_Comes_From_The_Shared_Runtime()
	{
		using var fixture = new PackageCacheFixture(nameof(When_HeadlessHead_Then_Everything_Comes_From_The_Shared_Runtime));

		// A headless head is a plain netX.0 project: no target platform, but a runtime host all the same.
		var (task, _) = CreateTask(fixture, targetPlatformIdentifier: "");

		task.Execute().Should().BeTrue();

		var added = Paths(task.RuntimeCopyLocalItemsToAdd).ToList();

		foreach (var assembly in WinRTAssemblies.Concat(OtherAssemblies))
		{
			added.Should().Contain(
				path => path.EndsWith($"uno-runtime/{NeutralTargetFramework}/skia/{assembly}.dll", StringComparison.Ordinal));
		}
	}

	[TestMethod]
	public void When_Platform_Has_No_Runtime_Assets_Then_It_Is_An_Error()
	{
		using var fixture = new PackageCacheFixture(nameof(When_Platform_Has_No_Runtime_Assets_Then_It_Is_An_Error));
		var (task, _) = CreateTask(fixture, targetPlatformIdentifier: "maccatalyst");

		task.Execute().Should().BeFalse();
		((RecordingBuildEngine)task.BuildEngine).Errors.Should().NotBeEmpty();
	}

	[TestMethod]
	public void When_AndroidHead_Then_A_Plain_Library_Asset_Is_Untouched()
	{
		using var fixture = new PackageCacheFixture(nameof(When_AndroidHead_Then_A_Plain_Library_Asset_Is_Untouched));
		var (task, _) = CreateTask(fixture, targetPlatformIdentifier: "android");

		task.Execute().Should().BeTrue();

		// The library ships only lib/netX.0 and is not runtime-enabled, so nothing may rewrite it.
		Paths(task.RuntimeCopyLocalItemsToRemove).Should().NotContain(path => path.Contains("Contoso.Sensors", StringComparison.Ordinal));
		Paths(task.ResolvedCompileFileDefinitionsToRemove).Should().NotContain(path => path.Contains("Contoso.Sensors", StringComparison.Ordinal));
		Paths(task.RuntimeCopyLocalItemsToAdd).Should().NotContain(path => path.Contains("Contoso.Sensors", StringComparison.Ordinal));
	}

	[TestMethod]
	public void When_AndroidHead_Then_Non_WinRT_Assemblies_Compile_Against_The_Neutral_Surface()
	{
		using var fixture = new PackageCacheFixture(nameof(When_AndroidHead_Then_Non_WinRT_Assemblies_Compile_Against_The_Neutral_Surface));
		var (task, _) = CreateTask(fixture, targetPlatformIdentifier: "android");

		task.Execute().Should().BeTrue();

		var compileAdded = Paths(task.ResolvedCompileFileDefinitionsToAdd).ToList();

		compileAdded.Should().Contain(
			path => path.EndsWith($"lib/{NeutralTargetFramework}/Contoso.CrossRuntime.dll", StringComparison.Ordinal),
			"a non-WinRT assembly keeps the union compile surface");

		compileAdded.Should().Contain(
			path => path.EndsWith($"lib/{AndroidTargetFramework}/Uno.WinRT.dll", StringComparison.Ordinal),
			"a WinRT assembly compiles against the platform implementation");
	}
}

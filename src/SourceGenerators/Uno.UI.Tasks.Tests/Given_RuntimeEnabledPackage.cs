using AwesomeAssertions;
using Microsoft.Build.Framework;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Covers the two-layer selection Uno.WinRT and Uno.Foundation rely on: the UI assemblies come from the Skia
/// runtime folder while the WinRT ones are redirected to the implementation matching the head's platform. This is
/// what lets a library built for a plain netX.0 call a WinRT API and still reach the platform implementation.
/// </summary>
[TestClass]
public class Given_RuntimeEnabledPackage
{
	private const string NeutralTargetFramework = "net10.0";
	private const string AndroidTargetFramework = "net10.0-android30.0";

	private static readonly string[] WinRTAssemblies = ["Uno", "Uno.Foundation", "Uno.UI.Dispatching"];
	private static readonly string[] OtherAssemblies = ["Contoso.CrossRuntime"];

	private static (RuntimeAssetsSelectorTask Task, string PackageBasePath) CreateTask(
		PackageCacheFixture fixture,
		string unoRuntimeIdentifier,
		string unoUIRuntimeIdentifier,
		string unoWinRTRuntimeIdentifier)
	{
		var packageBasePath = fixture.AddRuntimeEnabledPackage(
			"Uno.WinRT",
			"1.0.0",
			NeutralTargetFramework,
			AndroidTargetFramework,
			WinRTAssemblies,
			OtherAssemblies,
			["skia", "webassembly"]);

		// A plain netX.0 library: it only ships a platform-neutral asset and is not runtime-enabled.
		var plainLibrary = fixture.AddPackage("Contoso.Sensors", "2.0.0", "net10.0-android35.0", NeutralTargetFramework, []);
		var plainNeutralAsset = Path.Combine(fixture.Root, "Contoso.Sensors", "2.0.0", "lib", NeutralTargetFramework, "Contoso.Sensors.dll");

		var task = new RuntimeAssetsSelectorTask
		{
			BuildEngine = new RecordingBuildEngine(),
			UnoRuntimeEnabledPackage = [PackageCacheFixture.Item("Uno.WinRT", ("PackageBasePath", packageBasePath))],
			UnoRuntimeIdentifier = unoRuntimeIdentifier,
			UnoUIRuntimeIdentifier = unoUIRuntimeIdentifier,
			UnoWinRTRuntimeIdentifier = unoWinRTRuntimeIdentifier,
			TargetFrameworkVersion = "v10.0",
			ResolvedCompileFileDefinitionsInput =
			[
				PackageCacheFixture.Item(
					Path.Combine(fixture.Root, "Uno.WinRT", "1.0.0", "lib", NeutralTargetFramework, "Uno.dll"),
					("NuGetPackageId", "Uno.WinRT"), ("NuGetPackageVersion", "1.0.0")),
				PackageCacheFixture.Item(plainNeutralAsset, ("NuGetPackageId", "Contoso.Sensors")),
			],
			RuntimeCopyLocalItemsInput =
			[
				PackageCacheFixture.Item(
					Path.Combine(fixture.Root, "Uno.WinRT", "1.0.0", "lib", NeutralTargetFramework, "Uno.dll"),
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
	public void When_AndroidHead_Then_WinRT_Assemblies_Come_From_The_Platform_Implementation()
	{
		using var fixture = new PackageCacheFixture(nameof(When_AndroidHead_Then_WinRT_Assemblies_Come_From_The_Platform_Implementation));
		var (task, _) = CreateTask(fixture, unoRuntimeIdentifier: "", unoUIRuntimeIdentifier: "skia", unoWinRTRuntimeIdentifier: "android");

		task.Execute().Should().BeTrue();

		var added = Paths(task.RuntimeCopyLocalItemsToAdd).ToList();

		foreach (var winRTAssembly in WinRTAssemblies)
		{
			added.Should().Contain(
				path => path.EndsWith($"lib/{AndroidTargetFramework}/{winRTAssembly}.dll", StringComparison.Ordinal),
				$"{winRTAssembly} must resolve to the Android implementation");
		}

		// Everything else stays on the Skia build.
		added.Should().Contain(path => path.EndsWith($"uno-runtime/{NeutralTargetFramework}/skia/Contoso.CrossRuntime.dll", StringComparison.Ordinal));
		added.Should().NotContain(path => path.EndsWith($"uno-runtime/{NeutralTargetFramework}/skia/Uno.dll", StringComparison.Ordinal));
		added.Should().NotContain(path => path.Contains("/webassembly/", StringComparison.Ordinal));
	}

	[TestMethod]
	public void When_WebAssemblyHead_Then_WinRT_Assemblies_Come_From_The_WebAssembly_Runtime()
	{
		using var fixture = new PackageCacheFixture(nameof(When_WebAssemblyHead_Then_WinRT_Assemblies_Come_From_The_WebAssembly_Runtime));
		var (task, _) = CreateTask(fixture, unoRuntimeIdentifier: "", unoUIRuntimeIdentifier: "skia", unoWinRTRuntimeIdentifier: "webassembly");

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
		var (task, _) = CreateTask(fixture, unoRuntimeIdentifier: "skia", unoUIRuntimeIdentifier: "", unoWinRTRuntimeIdentifier: "");

		task.Execute().Should().BeTrue();

		var added = Paths(task.RuntimeCopyLocalItemsToAdd).ToList();

		foreach (var assembly in WinRTAssemblies.Concat(OtherAssemblies))
		{
			added.Should().Contain(
				path => path.EndsWith($"uno-runtime/{NeutralTargetFramework}/skia/{assembly}.dll", StringComparison.Ordinal));
		}

		// Single-layer heads keep compiling against the platform-neutral surface.
		task.ResolvedCompileFileDefinitionsToAdd.Should().BeEmpty();
		task.ResolvedCompileFileDefinitionsToRemove.Should().BeEmpty();
	}

	[TestMethod]
	public void When_AndroidHead_Then_A_Plain_Library_Asset_Is_Untouched()
	{
		using var fixture = new PackageCacheFixture(nameof(When_AndroidHead_Then_A_Plain_Library_Asset_Is_Untouched));
		var (task, _) = CreateTask(fixture, unoRuntimeIdentifier: "", unoUIRuntimeIdentifier: "skia", unoWinRTRuntimeIdentifier: "android");

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
		var (task, _) = CreateTask(fixture, unoRuntimeIdentifier: "", unoUIRuntimeIdentifier: "skia", unoWinRTRuntimeIdentifier: "android");

		task.Execute().Should().BeTrue();

		var compileAdded = Paths(task.ResolvedCompileFileDefinitionsToAdd).ToList();

		compileAdded.Should().Contain(
			path => path.EndsWith($"lib/{NeutralTargetFramework}/Contoso.CrossRuntime.dll", StringComparison.Ordinal),
			"a non-WinRT assembly keeps the union compile surface");

		compileAdded.Should().Contain(
			path => path.EndsWith($"lib/{AndroidTargetFramework}/Uno.dll", StringComparison.Ordinal),
			"a WinRT assembly compiles against the platform implementation");
	}
}

using AwesomeAssertions;
using Microsoft.Build.Framework;

namespace Uno.UI.Tasks.Tests;

[TestClass]
public class Given_RuntimeAssetsValidatorTask
{
	private const string RemovedType = "Uno.UI.Controls.BindableUIView";
	private const string PresentType = "Microsoft.UI.Xaml.UIElement";

	private static (RuntimeAssetsValidatorTask Task, RecordingBuildEngine Engine) CreateTask(
		PackageCacheFixture fixture,
		string asset,
		string targetPlatformIdentifier = "android",
		bool resolveUnoUI = true,
		bool disablePlatformAssetValidation = false,
		string? nuGetPackageRoot = null)
	{
		var engine = new RecordingBuildEngine();
		var unoUI = fixture.AddUnoUI(PresentType);

		var task = new RuntimeAssetsValidatorTask
		{
			BuildEngine = engine,
			UnoRuntimeIdentifier = "",
			UnoUIRuntimeIdentifier = "skia",
			UnoWinRTRuntimeIdentifier = "",
			TargetPlatformIdentifier = targetPlatformIdentifier,
			NuGetPackageRoot = nuGetPackageRoot ?? fixture.NuGetPackageRoot,
			DisablePlatformAssetValidation = disablePlatformAssetValidation,
			ResolvedCompileFileDefinitionsInput = resolveUnoUI ? [PackageCacheFixture.Item(unoUI)] : [],
			RuntimeCopyLocalItemsInput = [PackageCacheFixture.Item(asset, ("NuGetPackageId", "Sample.Lib"))],
		};

		return (task, engine);
	}

	private static BuildWarningEventArgs? Unob0020(RecordingBuildEngine engine)
		=> engine.Warnings.FirstOrDefault(warning => warning.Code == "UNOB0020");

	[TestMethod]
	[DataRow("android", "net10.0-android35.0")]
	[DataRow("ios", "net10.0-ios26.0")]
	[DataRow("tvos", "net10.0-tvos26.0")]
	public void When_PlatformAsset_References_A_Removed_Type_Then_UNOB0020(string targetPlatformIdentifier, string platformTargetFramework)
	{
		using var fixture = new PackageCacheFixture(nameof(When_PlatformAsset_References_A_Removed_Type_Then_UNOB0020));
		var asset = fixture.AddPackage("Sample.Lib", "1.0.0", platformTargetFramework, "net10.0", [RemovedType, PresentType]);

		var (task, engine) = CreateTask(fixture, asset, targetPlatformIdentifier);
		task.Execute().Should().BeTrue();

		var warning = Unob0020(engine);
		warning.Should().NotBeNull();
		warning!.Message.Should().Contain(RemovedType);
		warning.Message.Should().NotContain(PresentType);
		warning.Message.Should().Contain("Sample.Lib");
		engine.Errors.Should().BeEmpty();
	}

	[TestMethod]
	public void When_PlatformAsset_Is_Current_Then_No_Warning()
	{
		using var fixture = new PackageCacheFixture(nameof(When_PlatformAsset_Is_Current_Then_No_Warning));
		var asset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", [PresentType]);

		var (task, engine) = CreateTask(fixture, asset);
		task.Execute().Should().BeTrue();

		Unob0020(engine).Should().BeNull();
	}

	[TestMethod]
	public void When_Asset_Does_Not_Reference_UnoUI_Then_No_Warning()
	{
		using var fixture = new PackageCacheFixture(nameof(When_Asset_Does_Not_Reference_UnoUI_Then_No_Warning));
		var asset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", [], referencesUnoUI: false);

		var (task, engine) = CreateTask(fixture, asset);
		task.Execute().Should().BeTrue();

		Unob0020(engine).Should().BeNull();
	}

	[TestMethod]
	[DataRow("browserwasm")]
	[DataRow("desktop")]
	[DataRow("")]
	public void When_Head_Is_Not_Mobile_Then_No_Warning(string targetPlatformIdentifier)
	{
		using var fixture = new PackageCacheFixture(nameof(When_Head_Is_Not_Mobile_Then_No_Warning));
		var asset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", [RemovedType]);

		var (task, engine) = CreateTask(fixture, asset, targetPlatformIdentifier);
		task.Execute().Should().BeTrue();

		Unob0020(engine).Should().BeNull();
	}

	[TestMethod]
	public void When_UnoUI_Is_Not_Resolvable_Then_No_Warning()
	{
		using var fixture = new PackageCacheFixture(nameof(When_UnoUI_Is_Not_Resolvable_Then_No_Warning));
		var asset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", [RemovedType]);

		var (task, engine) = CreateTask(fixture, asset, resolveUnoUI: false);
		task.Execute().Should().BeTrue();

		Unob0020(engine).Should().BeNull();
		engine.Errors.Should().BeEmpty();
	}

	[TestMethod]
	public void When_Validation_Is_Disabled_Then_No_Warning()
	{
		using var fixture = new PackageCacheFixture(nameof(When_Validation_Is_Disabled_Then_No_Warning));
		var asset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", [RemovedType]);

		var (task, engine) = CreateTask(fixture, asset, disablePlatformAssetValidation: true);
		task.Execute().Should().BeTrue();

		Unob0020(engine).Should().BeNull();
	}

	[TestMethod]
	public void When_NuGetPackageRoot_Is_Unset_Then_No_Warning()
	{
		using var fixture = new PackageCacheFixture(nameof(When_NuGetPackageRoot_Is_Unset_Then_No_Warning));
		var asset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", [RemovedType]);

		var (task, engine) = CreateTask(fixture, asset, nuGetPackageRoot: "");
		task.Execute().Should().BeTrue();

		Unob0020(engine).Should().BeNull();
		engine.Errors.Should().BeEmpty();
	}

	[TestMethod]
	public void When_Stamp_Follows_Other_AssemblyMetadata_Then_It_Is_Still_Read()
	{
		using var fixture = new PackageCacheFixture(nameof(When_Stamp_Follows_Other_AssemblyMetadata_Then_It_Is_Still_Read));

		// The SDK emits IsTrimmable and RepositoryUrl ahead of Uno's own stamp.
		var asset = fixture.AddAssembly(
			"Sample.Lib",
			"1.0.0",
			"net10.0-android35.0",
			[PresentType],
			[("IsTrimmable", "True"), ("RepositoryUrl", "https://example.invalid"), ("UnoUIRuntimeIdentifier", "Skia")]);

		var (task, engine) = CreateTask(fixture, asset);
		task.UnoUIRuntimeIdentifier = "somethingelse";

		task.Execute().Should().BeFalse();
		engine.Errors.Should().ContainSingle();
		engine.Errors[0].Message.Should().Contain("Skia");
	}

	[TestMethod]
	public void When_SingleLayer_Head_Then_A_Skia_Stamp_Matches()
	{
		using var fixture = new PackageCacheFixture(nameof(When_SingleLayer_Head_Then_A_Skia_Stamp_Matches));

		var asset = fixture.AddAssembly(
			"Sample.Lib",
			"1.0.0",
			"net10.0-android35.0",
			[PresentType],
			[("IsTrimmable", "True"), ("UnoUIRuntimeIdentifier", "Skia")]);

		var (task, engine) = CreateTask(fixture, asset);

		// A desktop head names its UI runtime through UnoRuntimeIdentifier instead.
		task.UnoUIRuntimeIdentifier = "";
		task.UnoRuntimeIdentifier = "skia";

		task.Execute().Should().BeTrue();
		engine.Errors.Should().BeEmpty();
	}
}

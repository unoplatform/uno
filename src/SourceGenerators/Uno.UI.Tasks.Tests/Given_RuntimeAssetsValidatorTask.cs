using AwesomeAssertions;
using Microsoft.Build.Framework;
using Uno.UI.Tasks.RuntimeAssetsValidator;

namespace Uno.UI.Tasks.Tests;

[TestClass]
public class Given_RuntimeAssetsValidatorTask
{
	private const string RemovedType = "Uno.UI.Controls.BindableUIView";
	private const string PresentType = "Microsoft.UI.Xaml.UIElement";

	private static (RuntimeAssetsValidatorTask_v0 Task, RecordingBuildEngine Engine) CreateTask(
		PackageCacheFixture fixture,
		string asset,
		string targetPlatformIdentifier = "android",
		bool resolveUnoUI = true,
		bool disablePlatformAssetValidation = false,
		string? nuGetPackageRoot = null)
	{
		var engine = new RecordingBuildEngine();
		var unoUI = fixture.AddUnoUI(PresentType);

		var task = new RuntimeAssetsValidatorTask_v0
		{
			BuildEngine = engine,
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
	public void When_PlatformAsset_References_A_Removed_Type_Then_UNOB0020()
	{
		using var fixture = new PackageCacheFixture(nameof(When_PlatformAsset_References_A_Removed_Type_Then_UNOB0020));
		var asset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", [RemovedType, PresentType]);

		var (task, engine) = CreateTask(fixture, asset);
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
	public void When_Head_Is_Not_Mobile_Then_No_Warning()
	{
		using var fixture = new PackageCacheFixture(nameof(When_Head_Is_Not_Mobile_Then_No_Warning));
		var asset = fixture.AddPackage("Sample.Lib", "1.0.0", "net10.0-android35.0", "net10.0", [RemovedType]);

		var (task, engine) = CreateTask(fixture, asset, targetPlatformIdentifier: "browserwasm");
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

		// The SDK emits IsTrimmable and RepositoryUrl ahead of the stamp earlier releases wrote.
		var asset = fixture.AddAssembly(
			"Sample.Lib",
			"1.0.0",
			"net10.0-android35.0",
			[PresentType],
			[("IsTrimmable", "True"), ("RepositoryUrl", "https://example.invalid"), ("UnoUIRuntimeIdentifier", "Android")]);

		var (task, engine) = CreateTask(fixture, asset);

		// An assembly built for the native Android renderer, which no longer exists.
		task.Execute().Should().BeFalse();
		engine.Errors.Should().ContainSingle();
		engine.Errors[0].Message.Should().Contain("Android");
	}

	[TestMethod]
	public void When_Stamp_Names_The_Shared_UI_Runtime_Then_It_Matches()
	{
		using var fixture = new PackageCacheFixture(nameof(When_Stamp_Names_The_Shared_UI_Runtime_Then_It_Matches));

		var asset = fixture.AddAssembly(
			"Sample.Lib",
			"1.0.0",
			"net10.0-android35.0",
			[PresentType],
			[("IsTrimmable", "True"), ("UnoUIRuntimeIdentifier", "Skia")]);

		var (task, engine) = CreateTask(fixture, asset);

		task.Execute().Should().BeTrue();
		engine.Errors.Should().BeEmpty();
	}

	[TestMethod]
	public void When_There_Is_No_Stamp_Then_It_Is_Accepted()
	{
		using var fixture = new PackageCacheFixture(nameof(When_There_Is_No_Stamp_Then_It_Is_Accepted));

		// Uno Platform 7.0 stamps nothing, having a single UI runtime.
		var asset = fixture.AddAssembly("Sample.Lib", "1.0.0", "net10.0-android35.0", [PresentType], []);

		var (task, engine) = CreateTask(fixture, asset);

		task.Execute().Should().BeTrue();
		engine.Errors.Should().BeEmpty();
	}
}

using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Drives uno.winui.runtime-replace.targets itself rather than the task behind it: the variant mapping, which
/// layouts ReplaceUnoRuntime reads, and when UNOB0023, UNOB0024 and UNOB0025 fire.
/// </summary>
[TestClass]
public class Given_RuntimeReplaceTargets
{
	private const string NeutralTargetFramework = "net10.0";
	private const string AndroidTargetFramework = "net10.0-android30.0";

	public TestContext TestContext { get; set; } = null!;

	private RuntimeReplaceProject CreateProject(PackageCacheFixture fixture, string targetFramework = NeutralTargetFramework)
		=> new(Path.Combine(fixture.Root, "project"), targetFramework);

	private PackageCacheFixture CreateFixture() => new(TestContext.TestName!);

	private static string AddPackage(PackageCacheFixture fixture, string packageId, params string[] folders)
		=> fixture.AddRuntimeEnabledPackage(packageId, "1.0.0", NeutralTargetFramework, AndroidTargetFramework, [], [packageId], folders);

	/// <summary>The layout UnoRuntimeProjectReference packs: uno-runtime/&lt;variant&gt;, without a target framework.</summary>
	private static string AddCrossRuntimeLibrary(PackageCacheFixture fixture, string packageId, params string[] folders)
	{
		var packageRoot = Path.Combine(fixture.Root, packageId, "1.0.0");
		foreach (var folder in folders)
		{
			var path = Path.Combine(packageRoot, "uno-runtime", folder, packageId + ".dll");
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllBytes(path, []);
		}

		var packageBasePath = Path.Combine(packageRoot, "buildTransitive");
		Directory.CreateDirectory(packageBasePath);
		return packageBasePath;
	}

	[TestMethod]
	[DataRow("Generic", "generic")]
	[DataRow("Wasm", "wasm")]
	[DataRow("Reference", "reference")]
	public void When_Library_Sets_UnoRuntimeVariant_Then_The_Folder_Is_The_Value_Lowercased(string variant, string folder)
	{
		using var fixture = CreateFixture();

		var result = CreateProject(fixture).Property("UnoRuntimeVariant", variant)
			.Run("_UnoComputeLibraryRuntimeVariant", ["_LibraryUnoRuntimeVariant", "_UnoTaskLibraryRuntimeVariant"]);

		result.Succeeded.Should().BeTrue(result.Log);
		result.Property("_LibraryUnoRuntimeVariant").Should().Be(folder);
		result.Property("_UnoTaskLibraryRuntimeVariant").Should().Be(folder);
	}

	[TestMethod]
	[DataRow("Skia", "generic")]
	[DataRow("WebAssembly", "wasm")]
	[DataRow("Reference", "reference")]
	public void When_Library_Sets_The_Deprecated_UnoRuntimeIdentifier_Then_It_Maps_To_The_Variant(string identifier, string folder)
	{
		using var fixture = CreateFixture();

		var result = CreateProject(fixture).Property("UnoRuntimeIdentifier", identifier)
			.Run("_UnoComputeLibraryRuntimeVariant", ["_LibraryUnoRuntimeVariant"]);

		result.Property("_LibraryUnoRuntimeVariant").Should().Be(folder);
	}

	[TestMethod]
	public void When_Head_Has_A_Runtime_Host_Then_No_Library_Variant_Reaches_The_Task()
	{
		using var fixture = CreateFixture();

		var result = CreateProject(fixture)
			.Property("UnoHasRuntimeHost", "true")
			.Property("UnoRuntimeIdentifier", "WebAssembly")
			.Run("_UnoComputeLibraryRuntimeVariant", ["_LibraryUnoRuntimeVariant", "_UnoTaskLibraryRuntimeVariant"]);

		result.Property("_LibraryUnoRuntimeVariant").Should().Be("generic");
		result.Property("_UnoTaskLibraryRuntimeVariant").Should().BeEmpty();
	}

	[TestMethod]
	public void When_Head_References_A_Runtime_Enabled_Package_Then_Its_Runtime_Assemblies_Are_Deployed()
	{
		using var fixture = CreateFixture();
		var packageBasePath = AddPackage(fixture, "Contoso.Runtime", "generic", "wasm");

		var result = CreateProject(fixture)
			.Property("UnoHasRuntimeHost", "true")
			.RuntimeEnabledPackage("Contoso.Runtime", packageBasePath)
			.Run("ReplaceUnoRuntime", items: ["RuntimeCopyLocalItems"]);

		result.Succeeded.Should().BeTrue(result.Log);
		result.Items("RuntimeCopyLocalItems").Should().Contain(p => p.EndsWith("uno-runtime/net10.0/generic/Contoso.Runtime.dll", StringComparison.Ordinal));
	}

	[TestMethod]
	public void When_Package_Uses_The_Cross_Runtime_Library_Layout_Then_It_Is_Deployed()
	{
		using var fixture = CreateFixture();
		var packageBasePath = AddCrossRuntimeLibrary(fixture, "Contoso.CrossRuntime", "generic", "wasm");

		var result = CreateProject(fixture)
			.Property("UnoHasRuntimeHost", "true")
			.RuntimeEnabledPackage("Contoso.CrossRuntime", packageBasePath)
			.Run("ReplaceUnoRuntime", items: ["RuntimeCopyLocalItems"]);

		result.Succeeded.Should().BeTrue(result.Log);
		result.HasError("UNOB0023").Should().BeFalse();
		result.Items("RuntimeCopyLocalItems").Should().Contain(p => p.EndsWith("uno-runtime/generic/Contoso.CrossRuntime.dll", StringComparison.Ordinal));
	}

	[TestMethod]
	public void When_Package_Uses_The_Pre_7_Folder_Names_Then_UNOB0023_Fails_The_Build()
	{
		using var fixture = CreateFixture();
		var packageBasePath = AddPackage(fixture, "Contoso.Legacy", "skia", "webassembly");

		var result = CreateProject(fixture)
			.Property("UnoHasRuntimeHost", "true")
			.RuntimeEnabledPackage("Contoso.Legacy", packageBasePath)
			.Run("ReplaceUnoRuntime");

		result.Succeeded.Should().BeFalse();
		result.HasError("UNOB0023").Should().BeTrue(result.Log);
		result.Diagnostics("UNOB0023").Should().Contain(l => l.Contains("Contoso.Legacy", StringComparison.Ordinal));
	}

	[TestMethod]
	[DataRow("DesignTimeBuild")]
	[DataRow("UnoDisableUNOB0023Validation")]
	public void When_UNOB0023_Is_Suppressed_Then_The_Build_Continues(string property)
	{
		using var fixture = CreateFixture();
		var packageBasePath = AddPackage(fixture, "Contoso.Legacy", "skia", "webassembly");

		var result = CreateProject(fixture)
			.Property("UnoHasRuntimeHost", "true")
			.Property(property, "true")
			.RuntimeEnabledPackage("Contoso.Legacy", packageBasePath)
			.Run("ReplaceUnoRuntime");

		result.Succeeded.Should().BeTrue(result.Log);
		result.HasError("UNOB0023").Should().BeFalse();
	}

	[TestMethod]
	public void When_Library_Is_The_Reference_Variant_Then_Nothing_Is_Required()
	{
		using var fixture = CreateFixture();
		var packageBasePath = AddPackage(fixture, "Contoso.Legacy", "skia");

		var result = CreateProject(fixture)
			.Property("UnoRuntimeVariant", "Reference")
			.RuntimeEnabledPackage("Contoso.Legacy", packageBasePath)
			.Run("ReplaceUnoRuntime");

		result.Succeeded.Should().BeTrue(result.Log);
		result.HasError("UNOB0023").Should().BeFalse();
	}

	[TestMethod]
	[DataRow("UnoUIRuntimeIdentifier")]
	[DataRow("UnoWinRTRuntimeIdentifier")]
	[DataRow("UnoRuntimeIdentifier")]
	public void When_Head_Sets_A_Retired_Property_Then_UNOB0024_Warns(string property)
	{
		using var fixture = CreateFixture();

		var result = CreateProject(fixture)
			.Property("UnoHasRuntimeHost", "true")
			.Property(property, "Skia")
			.Run("_UnoWarnObsoleteRuntimeIdentifiers");

		result.HasWarning("UNOB0024").Should().BeTrue(result.Log);
	}

	[TestMethod]
	public void When_UnoRuntimeIdentifier_Is_The_Resizetizer_Shim_Then_UNOB0024_Is_Silent()
	{
		using var fixture = CreateFixture();

		var result = CreateProject(fixture)
			.Property("UnoHasRuntimeHost", "true")
			.Property("_UnoRuntimeIdentifierResizetizerShim", "true")
			.Property("UnoRuntimeIdentifier", "Skia")
			.Run("_UnoWarnObsoleteRuntimeIdentifiers");

		result.Succeeded.Should().BeTrue(result.Log);
		result.HasWarning("UNOB0024").Should().BeFalse(result.Log);
	}

	[TestMethod]
	public void When_Library_Sets_The_Deprecated_UnoRuntimeIdentifier_Then_UNOB0024_Names_The_Variant()
	{
		using var fixture = CreateFixture();

		var result = CreateProject(fixture)
			.Property("UnoRuntimeIdentifier", "webassembly")
			.Run("_UnoWarnObsoleteRuntimeIdentifiers");

		result.Diagnostics("UNOB0024").Should().Contain(l => l.Contains("UnoRuntimeVariant to 'Wasm'", StringComparison.Ordinal));
	}

	[TestMethod]
	public void When_Executable_Has_No_Runtime_Host_Then_UNOB0025_Warns()
	{
		using var fixture = CreateFixture();
		var packageBasePath = AddPackage(fixture, "Contoso.Runtime", "generic");

		var result = CreateProject(fixture)
			.Property("OutputType", "Exe")
			.RuntimeEnabledPackage("Contoso.Runtime", packageBasePath)
			.Run("_UnoWarnMissingRuntimeHost");

		result.Succeeded.Should().BeTrue(result.Log);
		result.HasWarning("UNOB0025").Should().BeTrue(result.Log);
	}

	[TestMethod]
	public void When_A_Pre_7_Runtime_Host_Is_Referenced_Then_UNOB0025_Fails_The_Build()
	{
		using var fixture = CreateFixture();
		var packageBasePath = AddPackage(fixture, "Contoso.Runtime", "generic");

		// What a 6.x Uno.WinUI.Runtime.Skia.Android sets in place of UnoHasRuntimeHost.
		var result = CreateProject(fixture, AndroidTargetFramework)
			.Property("OutputType", "Exe")
			.Property("UnoUIRuntimeIdentifier", "Skia")
			.Property("UnoWinRTRuntimeIdentifier", "Android")
			.RuntimeEnabledPackage("Contoso.Runtime", packageBasePath)
			.Run("_UnoWarnMissingRuntimeHost");

		result.Succeeded.Should().BeFalse();
		result.HasError("UNOB0025").Should().BeTrue(result.Log);
	}

	[TestMethod]
	public void When_Packing_UnoRuntimeProjectReferences_Then_Only_Generic_And_Wasm_Builds_Go_Under_uno_runtime()
	{
		using var fixture = CreateFixture();
		var project = CreateProject(fixture);

		// A variant build stand-in: GetTargetPath is all UnoRuntimeGetTargetPath needs from it.
		Directory.CreateDirectory(project.Directory);
		File.WriteAllText(
			Path.Combine(project.Directory, "Variant.proj"),
			$"""
			<Project>
				<Import Project="{RepositoryPaths.Get("build", "nuget", "uno.winui.runtime-replace.targets")}" />
				<Import Project="{RepositoryPaths.Get("build", "nuget", "uno.winui.cross-runtime.targets")}" />
				<Target Name="GetTargetPath">
					<ItemGroup>
						<TargetPathWithTargetPlatformMoniker Include="$(MSBuildProjectDirectory)/bin/$(UnoRuntimeVariant)/Contoso.dll" />
					</ItemGroup>
				</Target>
			</Project>
			""");

		var result = project
			.ImportCrossRuntimeTargets()
			.Property("BuildingProject", "true")
			.Item("<UnoRuntimeProjectReference Include=\"Variant.proj\" AdditionalProperties=\"UnoRuntimeVariant=Generic\" />")
			.Item("<UnoRuntimeProjectReference Include=\"Variant.proj\" AdditionalProperties=\"UnoRuntimeVariant=Wasm\" />")
			.Item("<UnoRuntimeProjectReference Include=\"Variant.proj\" AdditionalProperties=\"UnoRuntimeVariant=Reference\" />")
			.Raw("<Target Name=\"ResolveProjectReferences\" />")
			.Run("ResolvePrepareUnoRuntimeProjectReferences", items: ["TfmSpecificPackageFile"]);

		result.Succeeded.Should().BeTrue(result.Log);
		result.Items("TfmSpecificPackageFile", "PackagePath").Should().BeEquivalentTo(["uno-runtime/generic", "uno-runtime/wasm"]);
		result.Log.Should().Contain("UnoRuntimeVariant is 'reference', so it is not packed");
	}

	[TestMethod]
	public void When_A_Consumer_Hooks_The_Previous_Validation_Target_Then_The_Hook_Runs()
	{
		using var fixture = CreateFixture();

		var result = CreateProject(fixture)
			.Property("UnoHasRuntimeHost", "true")
			.Raw("<Target Name=\"ConsumerHook\" AfterTargets=\"_UnoValidateReferencesUnoRuntimeIdentifier\"><Message Importance=\"high\" Text=\"CONSUMER HOOK RAN\" /></Target>")
			.Run("CoreCompile");

		result.Succeeded.Should().BeTrue(result.Log);
		result.Log.Should().Contain("CONSUMER HOOK RAN");
	}
}

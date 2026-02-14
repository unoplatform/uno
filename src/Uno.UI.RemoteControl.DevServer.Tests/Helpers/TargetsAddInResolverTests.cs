using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class TargetsAddInResolverTests
{
	private static ILogger<TargetsAddInResolver> _logger = null!;
	private string _tempDir = null!;

	[ClassInitialize]
	public static void ClassInitialize(TestContext _)
	{
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.AddDebug();
			builder.SetMinimumLevel(LogLevel.Trace);
		});
		_logger = loggerFactory.CreateLogger<TargetsAddInResolver>();
	}

	[TestInitialize]
	public void TestInitialize()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "TargetsAddInResolverTests_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
	}

	[TestCleanup]
	public void TestCleanup()
	{
		try
		{
			if (Directory.Exists(_tempDir))
			{
				Directory.Delete(_tempDir, recursive: true);
			}
		}
		catch
		{
			// Best effort cleanup
		}
	}

	[TestMethod]
	public void ResolveAddIns_WhenSimpleDirectInclude_ShouldResolveDll()
	{
		// Arrange - Simulates: <UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/MyAddIn.dll" />
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.AddIn", "1.0.0"));

		var dllPath = CreatePackageWithTargets(
			nugetCache, "uno.test.addin", "1.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/MyAddIn.dll" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/MyAddIn.dll");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().HaveCount(1);
		results[0].PackageName.Should().Be("Uno.Test.AddIn");
		results[0].PackageVersion.Should().Be("1.0.0");
		results[0].EntryPointDll.Should().Be(dllPath);
		results[0].DiscoverySource.Should().Be("targets");
	}

	[TestMethod]
	public void ResolveAddIns_WhenPropertyIndirection_ShouldResolveDll()
	{
		// Arrange - Simulates property indirection pattern (like Uno.UI.App.Mcp)
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.Mcp", "2.0.0"));

		var dllPath = CreatePackageWithTargets(
			nugetCache, "uno.test.mcp", "2.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<PropertyGroup>
					<_TestProcessorPath>$(MSBuildThisFileDirectory)../tools/devserver/Uno.Test.Mcp.dll</_TestProcessorPath>
				</PropertyGroup>
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(_TestProcessorPath)" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/Uno.Test.Mcp.dll");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().HaveCount(1);
		results[0].EntryPointDll.Should().Be(dllPath);
	}

	[TestMethod]
	public void ResolveAddIns_WhenExistsConditionTrue_ShouldResolve()
	{
		// Arrange - Property with exists() condition that resolves to true
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.Exists", "1.0.0"));

		var dllPath = CreatePackageWithTargets(
			nugetCache, "uno.test.exists", "1.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<PropertyGroup>
					<_MyPath Condition="exists('$(MSBuildThisFileDirectory)../tools/devserver/Plugin.dll')">$(MSBuildThisFileDirectory)../tools/devserver/Plugin.dll</_MyPath>
				</PropertyGroup>
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(_MyPath)" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/Plugin.dll");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().HaveCount(1);
		results[0].EntryPointDll.Should().Be(dllPath);
	}

	[TestMethod]
	public void ResolveAddIns_WhenExistsConditionFalse_ShouldSkipProperty()
	{
		// Arrange - Property with exists() condition that resolves to false (DLL doesn't exist)
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.NoExists", "1.0.0"));

		CreatePackageWithTargets(
			nugetCache, "uno.test.noexists", "1.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<PropertyGroup>
					<_MyPath Condition="exists('$(MSBuildThisFileDirectory)../tools/devserver/NonExistent.dll')">$(MSBuildThisFileDirectory)../tools/devserver/NonExistent.dll</_MyPath>
				</PropertyGroup>
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(_MyPath)" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: null); // Don't create the DLL

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().BeEmpty();
	}

	[TestMethod]
	public void ResolveAddIns_WhenEqualityConditionMatches_ShouldResolve()
	{
		// Arrange - Item with MSBuildThisFile equality condition
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.Eq", "1.0.0"));

		var dllPath = CreatePackageWithTargets(
			nugetCache, "uno.test.eq", "1.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/Eq.dll" Condition="'$(MSBuildThisFile)'=='Uno.Test.Eq.targets'" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/Eq.dll",
			targetsFileName: "Uno.Test.Eq.targets");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().HaveCount(1);
	}

	[TestMethod]
	public void ResolveAddIns_WhenEqualityConditionDoesNotMatch_ShouldSkip()
	{
		// Arrange - Item with MSBuildThisFile condition that doesn't match
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.EqFail", "1.0.0"));

		CreatePackageWithTargets(
			nugetCache, "uno.test.eqfail", "1.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/Eq.dll" Condition="'$(MSBuildThisFile)'=='WrongName.targets'" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/Eq.dll",
			targetsFileName: "Uno.Test.EqFail.targets");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().BeEmpty();
	}

	[TestMethod]
	public void ResolveAddIns_WhenDllDoesNotExist_ShouldSkip()
	{
		// Arrange - .targets references a DLL that doesn't exist on disk
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.NoDll", "1.0.0"));

		CreatePackageWithTargets(
			nugetCache, "uno.test.nodll", "1.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/Missing.dll" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: null); // Don't create the DLL

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().BeEmpty();
	}

	[TestMethod]
	public void ResolveAddIns_WhenMalformedXml_ShouldSkipAndNotThrow()
	{
		// Arrange
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.BadXml", "1.0.0"));

		CreatePackageWithTargets(
			nugetCache, "uno.test.badxml", "1.0.0",
			"<this is not valid xml><<<",
			dllRelativePath: null);

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().BeEmpty();
	}

	[TestMethod]
	public void ResolveAddIns_WhenMultiplePackages_ShouldResolveAll()
	{
		// Arrange
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.AddIn.A", "1.0.0"),
			("Uno.AddIn.B", "2.0.0"));

		var dllA = CreatePackageWithTargets(
			nugetCache, "uno.addin.a", "1.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/A.dll" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/A.dll");

		var dllB = CreatePackageWithTargets(
			nugetCache, "uno.addin.b", "2.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/B.dll" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/B.dll");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().HaveCount(2);
		results.Select(r => r.PackageName).Should().BeEquivalentTo(["Uno.AddIn.A", "Uno.AddIn.B"]);
	}

	[TestMethod]
	public void ResolveAddIns_WhenPackageNotInCache_ShouldSkipSilently()
	{
		// Arrange - Package listed in packages.json but not in any cache
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Not.Installed", "1.0.0"));

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().BeEmpty();
	}

	[TestMethod]
	public void ResolveAddIns_WhenNoTargetsFiles_ShouldReturnEmpty()
	{
		// Arrange - Package exists but has no .targets files
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.No.Targets", "1.0.0"));

		// Create package directory without buildTransitive
		var packageDir = Path.Combine(nugetCache, "uno.no.targets", "1.0.0");
		Directory.CreateDirectory(packageDir);

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().BeEmpty();
	}

	[TestMethod]
	public void ResolveAddIns_WhenBuildFallback_ShouldScanBuildDirectory()
	{
		// Arrange - Package has build/*.targets but not buildTransitive/*.targets
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Build.Fallback", "1.0.0"));

		var packageDir = Path.Combine(nugetCache, "uno.build.fallback", "1.0.0");
		var buildDir = Path.Combine(packageDir, "build");
		Directory.CreateDirectory(buildDir);

		// Create DLL
		var toolsDir = Path.Combine(packageDir, "tools", "devserver");
		Directory.CreateDirectory(toolsDir);
		var dllPath = Path.Combine(toolsDir, "Fallback.dll");
		File.WriteAllBytes(dllPath, [0]);

		// Create .targets in build/ (not buildTransitive/)
		File.WriteAllText(
			Path.Combine(buildDir, "Uno.Build.Fallback.targets"),
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/Fallback.dll" />
				</ItemGroup>
			</Project>
			""");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().HaveCount(1);
		results[0].EntryPointDll.Should().Be(Path.GetFullPath(dllPath));
	}

	[TestMethod]
	public void ResolveAddIns_WhenNestedPropertyReferences_ShouldResolveUpTo5Levels()
	{
		// Arrange - Nested property references: $(A) -> $(B) -> value
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.Nested", "1.0.0"));

		var dllPath = CreatePackageWithTargets(
			nugetCache, "uno.test.nested", "1.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<PropertyGroup>
					<_Level1>$(MSBuildThisFileDirectory)../tools/devserver</_Level1>
					<_Level2>$(_Level1)/Nested.dll</_Level2>
				</PropertyGroup>
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(_Level2)" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/Nested.dll");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().HaveCount(1);
		results[0].EntryPointDll.Should().Be(dllPath);
	}

	[TestMethod]
	public void ResolveAddIns_WhenInvalidPackagesJson_ShouldReturnEmpty()
	{
		// Arrange
		var packagesJsonPath = Path.Combine(_tempDir, "invalid_packages.json");
		File.WriteAllText(packagesJsonPath, "this is not valid json");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [_tempDir]);

		// Assert
		results.Should().BeEmpty();
	}

	[TestMethod]
	public void ResolveAddIns_WhenEmptyPackagesJson_ShouldReturnEmpty()
	{
		// Arrange
		var packagesJsonPath = Path.Combine(_tempDir, "empty_packages.json");
		File.WriteAllText(packagesJsonPath, "[]");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [_tempDir]);

		// Assert
		results.Should().BeEmpty();
	}

	[TestMethod]
	public void ResolveAddIns_WhenInequalityCondition_ShouldEvaluateCorrectly()
	{
		// Arrange
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.Neq", "1.0.0"));

		var dllPath = CreatePackageWithTargets(
			nugetCache, "uno.test.neq", "1.0.0",
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<PropertyGroup>
					<_ShouldInclude Condition="'$(MSBuildThisFile)'!='wrong.targets'">$(MSBuildThisFileDirectory)../tools/devserver/Neq.dll</_ShouldInclude>
				</PropertyGroup>
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(_ShouldInclude)" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/Neq.dll",
			targetsFileName: "Uno.Test.Neq.targets");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().HaveCount(1);
	}

	[TestMethod]
	public void ResolveAddIns_WhenMultipleNuGetCaches_ShouldSearchAll()
	{
		// Arrange
		var cache1 = Path.Combine(_tempDir, "cache1");
		var cache2 = Path.Combine(_tempDir, "cache2");
		Directory.CreateDirectory(cache1);
		Directory.CreateDirectory(cache2);

		var packagesJsonPath = Path.Combine(_tempDir, "packages.json");
		File.WriteAllText(packagesJsonPath, """
			[{"version": "1.0.0", "packages": ["Uno.In.Cache2"]}]
			""");

		// Package only exists in cache2
		var packageDir = Path.Combine(cache2, "uno.in.cache2", "1.0.0");
		var buildDir = Path.Combine(packageDir, "buildTransitive");
		var toolsDir = Path.Combine(packageDir, "tools", "devserver");
		Directory.CreateDirectory(buildDir);
		Directory.CreateDirectory(toolsDir);

		var dllPath = Path.Combine(toolsDir, "Cache2.dll");
		File.WriteAllBytes(dllPath, [0]);

		File.WriteAllText(
			Path.Combine(buildDir, "Uno.In.Cache2.targets"),
			"""
			<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/Cache2.dll" />
				</ItemGroup>
			</Project>
			""");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [cache1, cache2]);

		// Assert
		results.Should().HaveCount(1);
		results[0].EntryPointDll.Should().Be(Path.GetFullPath(dllPath));
	}

	[TestMethod]
	public void ResolveAddIns_WhenNoNamespace_ShouldStillParse()
	{
		// Arrange - .targets file without namespace (some packages do this)
		var (packagesJsonPath, nugetCache) = CreatePackagesJson(
			("Uno.Test.NoNs", "1.0.0"));

		var dllPath = CreatePackageWithTargets(
			nugetCache, "uno.test.nons", "1.0.0",
			"""
			<Project>
				<ItemGroup>
					<UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/NoNs.dll" />
				</ItemGroup>
			</Project>
			""",
			dllRelativePath: "tools/devserver/NoNs.dll");

		var resolver = new TargetsAddInResolver(_logger);

		// Act
		var results = resolver.ResolveAddIns(packagesJsonPath, [nugetCache]);

		// Assert
		results.Should().HaveCount(1);
	}

	#region Test Helpers

	private (string packagesJsonPath, string nugetCache) CreatePackagesJson(
		params (string name, string version)[] packages)
	{
		var nugetCache = Path.Combine(_tempDir, "nuget_cache");
		Directory.CreateDirectory(nugetCache);

		// Group packages by version for the JSON format
		var groups = packages
			.GroupBy(p => p.version)
			.Select(g => new
			{
				version = g.Key,
				packages = g.Select(p => p.name).ToArray()
			});

		var json = System.Text.Json.JsonSerializer.Serialize(groups);
		var packagesJsonPath = Path.Combine(_tempDir, "packages.json");
		File.WriteAllText(packagesJsonPath, json);

		return (packagesJsonPath, nugetCache);
	}

	private string CreatePackageWithTargets(
		string nugetCache,
		string lowercasePackageName,
		string version,
		string targetsContent,
		string? dllRelativePath,
		string? targetsFileName = null)
	{
		var packageDir = Path.Combine(nugetCache, lowercasePackageName, version);
		var buildDir = Path.Combine(packageDir, "buildTransitive");
		Directory.CreateDirectory(buildDir);

		targetsFileName ??= lowercasePackageName.Replace(".", ".") + ".targets";
		// Use PascalCase-ish name for the targets file (matching NuGet convention)
		var targetsFilePath = Path.Combine(buildDir, targetsFileName);
		File.WriteAllText(targetsFilePath, targetsContent);

		if (dllRelativePath is not null)
		{
			var dllFullPath = Path.Combine(packageDir, dllRelativePath);
			var dllDir = Path.GetDirectoryName(dllFullPath)!;
			Directory.CreateDirectory(dllDir);
			File.WriteAllBytes(dllFullPath, [0]); // Create empty file
			return Path.GetFullPath(dllFullPath);
		}

		return "";
	}

	#endregion
}

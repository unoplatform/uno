using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class NuGetCacheHelperTests
{
	private string? _originalNugetPackages;
	private string _tempDir = null!;

	[TestInitialize]
	public void TestInitialize()
	{
		_originalNugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
		_tempDir = Path.Combine(Path.GetTempPath(), $"nuget-cache-tests-{Guid.NewGuid()}");
		Directory.CreateDirectory(_tempDir);
	}

	[TestCleanup]
	public void TestCleanup()
	{
		if (_originalNugetPackages is not null)
		{
			Environment.SetEnvironmentVariable("NUGET_PACKAGES", _originalNugetPackages);
		}
		else
		{
			Environment.SetEnvironmentVariable("NUGET_PACKAGES", null);
		}

		try
		{
			if (Directory.Exists(_tempDir))
			{
				Directory.Delete(_tempDir, true);
			}
		}
		catch
		{
			// Best-effort cleanup
		}
	}

	[TestMethod]
	public void GetNuGetCachePaths_CustomPathIncluded()
	{
		Environment.SetEnvironmentVariable("NUGET_PACKAGES", "/custom/path");

		var paths = NuGetCacheHelper.GetNuGetCachePaths();

		paths.Should().Contain("/custom/path");
	}

	[TestMethod]
	public void GetNuGetCachePaths_EmptyStringSkipped()
	{
		Environment.SetEnvironmentVariable("NUGET_PACKAGES", "");

		var paths = NuGetCacheHelper.GetNuGetCachePaths();

		paths.Should().NotContain("");
	}

	[TestMethod]
	public void GetNuGetCachePaths_WhitespaceSkipped()
	{
		Environment.SetEnvironmentVariable("NUGET_PACKAGES", "  ");

		var paths = NuGetCacheHelper.GetNuGetCachePaths();

		paths.Should().NotContain("  ");
	}

	[TestMethod]
	public void GetNuGetCachePaths_UnsetSkipped()
	{
		Environment.SetEnvironmentVariable("NUGET_PACKAGES", null);

		var paths = NuGetCacheHelper.GetNuGetCachePaths();

		// Should have exactly 2 default paths (user profile + common app data)
		paths.Should().HaveCount(2);
	}

	[TestMethod]
	public void GetNuGetCachePaths_AlwaysHasDefaultProfilePath()
	{
		Environment.SetEnvironmentVariable("NUGET_PACKAGES", null);

		var paths = NuGetCacheHelper.GetNuGetCachePaths();

		var expectedProfilePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".nuget", "packages");

		paths.Should().Contain(expectedProfilePath);
	}

	[TestMethod]
	public void GetNuGetCachePaths_EnvVarHasHighestPriority()
	{
		Environment.SetEnvironmentVariable("NUGET_PACKAGES", "/env/packages");

		var paths = NuGetCacheHelper.GetNuGetCachePaths();

		paths[0].Should().Be("/env/packages");
	}

	[TestMethod]
	public void GetNuGetCachePaths_NuGetConfig_AbsoluteGlobalPackagesFolder()
	{
		Environment.SetEnvironmentVariable("NUGET_PACKAGES", null);

		var customPath = Path.Combine(_tempDir, "custom-packages");
		WriteNuGetConfig(_tempDir, customPath);

		var paths = NuGetCacheHelper.GetNuGetCachePaths(_tempDir);

		paths.Should().Contain(customPath);
	}

	[TestMethod]
	public void GetNuGetCachePaths_NuGetConfig_RelativeGlobalPackagesFolder()
	{
		Environment.SetEnvironmentVariable("NUGET_PACKAGES", null);

		WriteNuGetConfig(_tempDir, "my-packages");

		var paths = NuGetCacheHelper.GetNuGetCachePaths(_tempDir);

		var expected = Path.GetFullPath(Path.Combine(_tempDir, "my-packages"));
		paths.Should().Contain(expected);
	}

	[TestMethod]
	public void GetNuGetCachePaths_NuGetConfig_WalksUpDirectoryTree()
	{
		Environment.SetEnvironmentVariable("NUGET_PACKAGES", null);

		var subDir = Path.Combine(_tempDir, "sub", "project");
		Directory.CreateDirectory(subDir);

		var customPath = Path.Combine(_tempDir, "repo-packages");
		WriteNuGetConfig(_tempDir, customPath);

		var paths = NuGetCacheHelper.GetNuGetCachePaths(subDir);

		paths.Should().Contain(customPath);
	}

	[TestMethod]
	public void GetNuGetCachePaths_NuGetConfig_NoDuplicates()
	{
		var defaultUserProfile = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".nuget", "packages");

		Environment.SetEnvironmentVariable("NUGET_PACKAGES", null);

		// nuget.config pointing to the same default path
		WriteNuGetConfig(_tempDir, defaultUserProfile);

		var paths = NuGetCacheHelper.GetNuGetCachePaths(_tempDir);

		paths.Where(p => string.Equals(p, defaultUserProfile, StringComparison.OrdinalIgnoreCase))
			.Should().HaveCount(1);
	}

	[TestMethod]
	public void TryGetGlobalPackagesFolderFromConfig_NoConfig_ReturnsNull()
	{
		var result = NuGetCacheHelper.TryGetGlobalPackagesFolderFromConfig(_tempDir);

		result.Should().BeNull();
	}

	[TestMethod]
	public void TryGetGlobalPackagesFolderFromConfig_NoConfigSection_ReturnsNull()
	{
		File.WriteAllText(
			Path.Combine(_tempDir, "nuget.config"),
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<configuration>
				<packageSources>
					<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
				</packageSources>
			</configuration>
			""");

		var result = NuGetCacheHelper.TryGetGlobalPackagesFolderFromConfig(_tempDir);

		result.Should().BeNull();
	}

	[TestMethod]
	public void TryGetGlobalPackagesFolderFromConfig_MalformedXml_ReturnsNull()
	{
		File.WriteAllText(Path.Combine(_tempDir, "nuget.config"), "not xml at all");

		var result = NuGetCacheHelper.TryGetGlobalPackagesFolderFromConfig(_tempDir);

		result.Should().BeNull();
	}

	private static void WriteNuGetConfig(string directory, string globalPackagesFolder)
	{
		var content = $"""
			<?xml version="1.0" encoding="utf-8"?>
			<configuration>
				<config>
					<add key="globalPackagesFolder" value="{globalPackagesFolder}" />
				</config>
			</configuration>
			""";
		File.WriteAllText(Path.Combine(directory, "nuget.config"), content);
	}
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class NuGetCacheHelperTests
{
	private string? _originalNugetPackages;

	[TestInitialize]
	public void TestInitialize()
	{
		_originalNugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
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
}

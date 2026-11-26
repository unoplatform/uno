using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// The HotTesting feature references Uno.HotTesting in non-optimized builds of the Uno Platform targets, and leaves the
/// tests folder out of every build without it: optimized builds, UnoDisableHotTesting, and WinAppSDK targets.
/// </summary>
[TestClass]
public class Given_UnoSdkHotTestingFeature
{
	private const string Windows = "net10.0-windows10.0.19041.0";
	private const string Desktop = "net10.0-desktop";
	private const string Package = "Uno.HotTesting";

	[TestMethod]
	[DataRow(Desktop)]
	[DataRow("net10.0-browserwasm")]
	[DataRow("net10.0-android")]
	[DataRow("net10.0-ios")]
	public void When_HotTesting_Is_Listed_Then_The_Package_Is_Referenced(string targetFramework)
	{
		Evaluate("HotTesting", targetFramework).Should().Contain(Package);
	}

	[TestMethod]
	[DataRow("", Desktop, "")]
	[DataRow("HotTesting", Desktop, "Optimize=true")]
	[DataRow("HotTesting", Desktop, "UnoDisableHotTesting=true")]
	[DataRow("HotTesting", Windows, "")]
	public void When_Not_Listed_Optimized_Disabled_Or_Windows_Then_The_Package_Is_Not_Referenced(string features, string targetFramework, string property)
	{
		Evaluate(features, targetFramework, property).Should().NotContain(Package);
	}

	[TestMethod]
	[DataRow(Desktop, "Optimize=true")]
	[DataRow(Desktop, "UnoDisableHotTesting=true")]
	[DataRow(Windows, "")]
	public void When_The_Package_Is_Not_Referenced_Then_The_Tests_Folder_Is_Excluded(string targetFramework, string property)
	{
		DefaultItemExcludes("HotTesting", targetFramework, property).Should().Contain("Tests/**");
	}

	[TestMethod]
	public void When_Debug_On_Desktop_Then_The_Tests_Folder_Is_Compiled()
	{
		DefaultItemExcludes("HotTesting", Desktop, "").Should().NotContain("Tests/**");
	}

	[TestMethod]
	public void When_A_Custom_Tests_Folder_Is_Set_Then_That_Folder_Is_Excluded()
	{
		var excludes = DefaultItemExcludes("HotTesting", Desktop, "Optimize=true", "HotTestingTestsFolder=AppTests");

		excludes.Should().Contain("AppTests/**");
		excludes.Should().NotContain("Tests/**");
	}

	private static string[] Evaluate(string features, string targetFramework, params string[] properties)
	{
		var result = RunProject(features, targetFramework, properties, "-getItem:PackageReference");
		result.ExitCode.Should().Be(0, result.Output);

		JsonDocument json;
		try
		{
			json = JsonDocument.Parse(result.StandardOutput);
		}
		catch (JsonException e)
		{
			throw new InvalidOperationException($"dotnet msbuild did not print JSON:{Environment.NewLine}{result.Output}", e);
		}

		using (json)
		{
			if (!json.RootElement.GetProperty("Items").TryGetProperty("PackageReference", out var references))
			{
				return [];
			}

			return references
				.EnumerateArray()
				.Select(item => item.GetProperty("Identity").GetString()!)
				.ToArray();
		}
	}

	private static string[] DefaultItemExcludes(string features, string targetFramework, params string[] properties)
	{
		var result = RunProject(features, targetFramework, properties, "-getProperty:DefaultItemExcludes");
		result.ExitCode.Should().Be(0, result.Output);

		return result.StandardOutput
			.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(entry => entry.Replace('\\', '/'))
			.ToArray();
	}

	private static DotnetMSBuild.Result RunProject(string features, string targetFramework, string[] properties, string query)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-hottesting-").FullName;
		try
		{
			// Uno.DefaultItems.targets reads the platform properties and AssetsFolder, which Sdk.props sets.
			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>{targetFramework}</TargetFramework>
						<OutputType>Exe</OutputType>
						<UnoSingleProject>true</UnoSingleProject>
						<UnoFeatures>{features}</UnoFeatures>
						<AssetsFolder>Assets\</AssetsFolder>
						<DisableDefaultUnoResizetizerItems>true</DisableDefaultUnoResizetizerItems>
					</PropertyGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.IsPlatform.Before.props")}" />
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.IsPlatform.props")}" />
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets")}" />
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.DefaultItems.targets")}" />
				</Project>
				""");

			string[] arguments = [projectPath, query, .. properties.Where(p => p.Length > 0).Select(p => $"-p:{p}")];

			// From the repository root, so its global.json picks the SDK.
			return DotnetMSBuild.Run(RepositoryPaths.Root, arguments);
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}

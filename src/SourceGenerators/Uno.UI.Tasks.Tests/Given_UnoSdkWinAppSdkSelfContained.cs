using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// An MSIX declares the Windows App Runtime as a framework dependency, so bundling it only adds size. An unpackaged
/// app has no such dependency, and needs the bundled runtime unless the app installs or bootstraps one itself.
/// </summary>
[TestClass]
public class Given_UnoSdkWinAppSdkSelfContained
{
	private const string Windows = "net10.0-windows10.0.19041.0";

	[TestMethod]
	[DataRow("MSIX", "", "")]
	[DataRow("None", "", "true")]
	[DataRow("", "", "true")]
	[DataRow("MSIX", "true", "true")]
	[DataRow("None", "false", "false")]
	public void When_Head_Is_Evaluated_Then_The_Windows_App_SDK_Is_Bundled_Only_If_Unpackaged(string packageType, string selfContained, string expected)
		=> Evaluate("WinExe", packageType, selfContained).Should().Be(expected);

	[TestMethod]
	public void When_Project_Has_An_Appx_Manifest_Then_It_Is_Packaged_And_Not_Bundled()
		=> Evaluate("WinExe", "", "", withManifest: true).Should().BeEmpty();

	[TestMethod]
	[DataRow("None")]
	[DataRow("MSIX")]
	public void When_Project_Is_A_Library_Then_The_Windows_App_SDK_Is_Not_Bundled(string packageType)
		=> Evaluate("Library", packageType, "").Should().BeEmpty();

	private static string Evaluate(string outputType, string packageType, string selfContained, bool withManifest = false)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-selfcontained-").FullName;
		try
		{
			if (withManifest)
			{
				File.WriteAllText(Path.Join(directory, "Package.appxmanifest"), "<Package />");
			}

			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>{Windows}</TargetFramework>
						<OutputType>{outputType}</OutputType>
						<UnoSingleProject>true</UnoSingleProject>
						<WindowsPackageType>{packageType}</WindowsPackageType>
						<WindowsAppSDKSelfContained>{selfContained}</WindowsAppSDKSelfContained>
					</PropertyGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets")}" />
				</Project>
				""");

			// From the repository root, so its global.json picks the SDK.
			var result = DotnetMSBuild.Run(RepositoryPaths.Root, [projectPath, "-getProperty:WindowsAppSDKSelfContained", "-getProperty:WindowsPackageType"]);
			result.ExitCode.Should().Be(0, result.Output);

			using var json = JsonDocument.Parse(result.StandardOutput);
			return json.RootElement.GetProperty("Properties").GetProperty("WindowsAppSDKSelfContained").GetString()!;
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}

using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Uno.Microsoft.UI.Reactor has no WinAppSDK build: the feature references it on the Uno Platform targets only,
/// and a Windows target fails with UNOB0031 instead of a CS0246 for every Reactor type.
/// </summary>
[TestClass]
public class Given_UnoSdkReactorFeature
{
	private const string Windows = "net10.0-windows10.0.19041.0";
	private const string Package = "Uno.Microsoft.UI.Reactor";

	[TestMethod]
	[DataRow("net10.0-desktop")]
	[DataRow("net10.0-browserwasm")]
	[DataRow("net10.0-android")]
	[DataRow("net10.0-ios")]
	public void When_Reactor_Is_Listed_Then_The_Package_Is_Referenced(string targetFramework)
	{
		Evaluate("Reactor", targetFramework).Should().Contain(Package);
	}

	[TestMethod]
	[DataRow("", "net10.0-desktop")]
	[DataRow("Reactor", Windows)]
	public void When_Reactor_Is_Not_Listed_Or_Targets_Windows_Then_The_Package_Is_Not_Referenced(string features, string targetFramework)
	{
		Evaluate(features, targetFramework).Should().NotContain(Package);
	}

	[TestMethod]
	[DataRow("Reactor", Windows, true)]
	[DataRow("", Windows, false)]
	[DataRow("Reactor", "net10.0-desktop", false)]
	public void When_Compiling_Then_Reactor_On_Windows_Is_An_Error(string features, string targetFramework, bool expected)
	{
		var result = RunProject(features, targetFramework, ["-t:CoreCompile"]);

		if (expected)
		{
			result.ExitCode.Should().NotBe(0, result.Output);
			result.Output.Should().Contain("error UNOB0031");
		}
		else
		{
			result.ExitCode.Should().Be(0, result.Output);
			result.Output.Should().NotContain("UNOB0031");
		}
	}

	private static string[] Evaluate(string features, string targetFramework)
	{
		var result = RunProject(features, targetFramework, ["-getItem:PackageReference"]);
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
			return json.RootElement.GetProperty("Items").GetProperty("PackageReference")
				.EnumerateArray()
				.Select(item => item.GetProperty("Identity").GetString()!)
				.ToArray();
		}
	}

	private static DotnetMSBuild.Result RunProject(string features, string targetFramework, string[] arguments)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-reactor-").FullName;
		try
		{
			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>{targetFramework}</TargetFramework>
						<OutputType>Exe</OutputType>
						<UnoSingleProject>true</UnoSingleProject>
						<UnoFeatures>{features}</UnoFeatures>
					</PropertyGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets")}" />
					<Target Name="CoreCompile" />
				</Project>
				""");

			// From the repository root, so its global.json picks the SDK.
			return DotnetMSBuild.Run(RepositoryPaths.Root, [projectPath, .. arguments]);
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}

using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// The Uno.Sdk no longer references Prism: a listed Prism feature adds no package and is reported as UNOB0035,
/// instead of surfacing as an unknown feature or as CS0246 errors only.
/// </summary>
[TestClass]
public class Given_UnoSdkPrismFeature
{
	private static readonly string[] PrismPackages = ["Prism.Uno.WinUI", "Prism.DryIoc.Uno.WinUI", "Prism.Uno.WinUI.Markup"];

	[TestMethod]
	[DataRow("Exe", "Prism")]
	[DataRow("Exe", "Prism;CSharpMarkup")]
	[DataRow("Library", "Prism")]
	public void When_Prism_Is_Listed_Then_No_Prism_Package_Is_Referenced(string outputType, string features)
	{
		var (unoFeatures, packages) = Evaluate(outputType, features);

		unoFeatures.Should().NotContain(";prism;");
		packages.Should().NotContain(PrismPackages);
	}

	[TestMethod]
	public void When_Prism_And_Navigation_Are_Listed_Then_Navigation_Is_Referenced()
	{
		var (_, packages) = Evaluate("Exe", "Prism;Navigation");

		packages.Should().Contain("Uno.Extensions.Navigation.WinUI");
	}

	[TestMethod]
	[DataRow("Exe", "Prism", true)]
	[DataRow("Library", "prism", true)]
	[DataRow("Exe", "Mvvm;Prism", true)]
	[DataRow("Exe", "Mvvm", false)]
	public void When_Compiling_Then_A_Listed_Prism_Is_Reported(string outputType, string features, bool expected)
	{
		var result = RunProject(outputType, features, ["-t:CoreCompile"]);

		result.ExitCode.Should().Be(0, result.Output);
		if (expected)
		{
			result.Output.Should().Contain("warning UNOB0035");
		}
		else
		{
			result.Output.Should().NotContain("UNOB0035");
		}
	}

	private static (string UnoFeatures, string[] Packages) Evaluate(string outputType, string features)
	{
		var result = RunProject(outputType, features, ["-getProperty:UnoFeatures", "-getItem:PackageReference"]);
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
			var unoFeatures = json.RootElement.GetProperty("Properties").GetProperty("UnoFeatures").GetString()!;
			var packages = json.RootElement.GetProperty("Items").GetProperty("PackageReference")
				.EnumerateArray()
				.Select(item => item.GetProperty("Identity").GetString()!)
				.ToArray();

			return (unoFeatures, packages);
		}
	}

	private static DotnetMSBuild.Result RunProject(string outputType, string features, string[] arguments)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-prism-").FullName;
		try
		{
			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>net10.0-desktop</TargetFramework>
						<OutputType>{outputType}</OutputType>
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

using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// UnoGenerateAotProfile on a WebAssembly or Android head needs an optimized build: without Optimize=true, compiling
/// fails with UNOB0007, not with an MSBuild error about the Error task itself.
/// </summary>
[TestClass]
public class Given_UnoSdkAotProfileValidation
{
	private const string BrowserWasm = "net10.0-browserwasm";
	private const string Android = "net10.0-android";
	private const string Desktop = "net10.0-desktop";
	private const string Windows = "net10.0-windows10.0.19041.0";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26767")]
	[DataRow(BrowserWasm)]
	[DataRow(Android)]
	public void When_Generating_An_Aot_Profile_Without_Optimize_Then_UNOB0007_Is_Reported(string targetFramework)
	{
		var result = RunProject(targetFramework, generateAotProfile: true, optimize: false);

		result.ExitCode.Should().NotBe(0, result.Output);
		result.Output.Should().Contain("error UNOB0007");

		// The Error task has no ErrorCode parameter: using it failed the task before it could report UNOB0007.
		result.Output.Should().NotContain("MSB4064");
		result.Output.Should().NotContain("MSB4063");
	}

	[TestMethod]
	[DataRow(BrowserWasm, true, true)]
	[DataRow(Android, true, true)]
	[DataRow(BrowserWasm, false, false)]
	[DataRow(Desktop, true, false)]
	[DataRow(Windows, true, false)]
	public void When_Optimized_Not_Generating_Or_Not_WebAssembly_Or_Android_Then_UNOB0007_Is_Not_Reported(string targetFramework, bool generateAotProfile, bool optimize)
	{
		var result = RunProject(targetFramework, generateAotProfile, optimize);

		result.ExitCode.Should().Be(0, result.Output);
		result.Output.Should().NotContain("UNOB0007");
	}

	private static DotnetMSBuild.Result RunProject(string targetFramework, bool generateAotProfile, bool optimize)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-aot-profile-").FullName;
		try
		{
			// The validation reads the platform properties, which Sdk.props sets. IntermediateOutputPath keeps files
			// written by the other CoreCompile hooks, such as the WebAssembly global usings mock, in the test folder.
			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>{targetFramework}</TargetFramework>
						<OutputType>Exe</OutputType>
						<UnoSingleProject>true</UnoSingleProject>
						<AssemblyName>Project</AssemblyName>
						<IntermediateOutputPath>$(MSBuildProjectDirectory)\obj\</IntermediateOutputPath>
						<UnoGenerateAotProfile>{(generateAotProfile ? "true" : "")}</UnoGenerateAotProfile>
						<Optimize>{(optimize ? "true" : "false")}</Optimize>
					</PropertyGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.IsPlatform.Before.props")}" />
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.IsPlatform.props")}" />
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets")}" />
					<Target Name="CoreCompile" />
				</Project>
				""");

			// From the repository root, so its global.json picks the SDK.
			return DotnetMSBuild.Run(RepositoryPaths.Root, [projectPath, "-t:CoreCompile"]);
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}

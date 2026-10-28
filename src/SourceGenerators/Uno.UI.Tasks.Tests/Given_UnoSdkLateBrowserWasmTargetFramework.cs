using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Sdk.props picks the WebAssembly SDK from a global TargetFramework only (-f, or the inner build of TargetFrameworks).
/// A head that sets a browserwasm TargetFramework itself would silently build as a desktop executable, so it fails
/// with UNOB0032 instead.
/// </summary>
[TestClass]
public class Given_UnoSdkLateBrowserWasmTargetFramework
{
	private const string BrowserWasm = "net10.0-browserwasm";

	[TestMethod]
	[DataRow("Exe", BrowserWasm, false, true)]
	[DataRow("WinExe", BrowserWasm, false, true)]
	[DataRow("Exe", BrowserWasm, true, false)]
	[DataRow("Library", BrowserWasm, false, false)]
	[DataRow("Exe", "net10.0-desktop", false, false)]
	public void When_TargetFramework_Is_Set_After_Sdk_Props_Then_A_BrowserWasm_Head_Is_An_Error(string outputType, string targetFramework, bool isGlobal, bool expected)
	{
		var result = RunProject(outputType, targetFramework, isGlobal);

		if (expected)
		{
			result.ExitCode.Should().NotBe(0, result.Output);
			result.Output.Should().Contain("error UNOB0032");
		}
		else
		{
			result.ExitCode.Should().Be(0, result.Output);
			result.Output.Should().NotContain("UNOB0032");
		}
	}

	private static DotnetMSBuild.Result RunProject(string outputType, string targetFramework, bool isGlobal)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-browserwasm-").FullName;
		try
		{
			// Imports the SDK files the way an MSBuild SDK does, so the project body is evaluated between them.
			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "Sdk", "Sdk.props")}" />
					<PropertyGroup>
						<TargetFramework>{targetFramework}</TargetFramework>
						<OutputType>{outputType}</OutputType>
						<UnoSingleProject>true</UnoSingleProject>
					</PropertyGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "Sdk", "Sdk.targets")}" />
				</Project>
				""");

			string[] arguments = isGlobal
				? [projectPath, "-t:UnoFailOnLateBrowserWasmTargetFramework", $"-p:TargetFramework={targetFramework}"]
				: [projectPath, "-t:UnoFailOnLateBrowserWasmTargetFramework"];

			// From the repository root, so its global.json picks the SDK.
			return DotnetMSBuild.Run(RepositoryPaths.Root, arguments);
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}

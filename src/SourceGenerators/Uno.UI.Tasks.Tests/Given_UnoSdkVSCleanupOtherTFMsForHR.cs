using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// When Visual Studio triggers a build of a head (for Hot Reload), the Uno.Sdk deletes the head's assembly from the
/// intermediate folders of its other target frameworks (https://github.com/dotnet/maui/issues/27612).
/// UnoDisableVSCleanupOtherTFMsForHR=true opts out of it.
/// </summary>
[TestClass]
public class Given_UnoSdkVSCleanupOtherTFMsForHR
{
	private const string TargetFramework = "net10.0-desktop";
	private const string OtherTargetFramework = "net10.0-android";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26767")]
	[DataRow("", true)]
	[DataRow("false", true)]
	[DataRow("true", false)]
	public void When_Visual_Studio_Triggers_A_Build_Then_UnoDisableVSCleanupOtherTFMsForHR_Opts_Out(string disable, bool expectDeleted)
	{
		var (other, current) = RunCleanup(insideVisualStudio: true, disable);

		other.Should().Be(!expectDeleted, "the assembly of the other target framework should {0}", expectDeleted ? "be deleted" : "be kept");
		current.Should().BeTrue("the assembly of the target framework being built is never deleted");
	}

	[TestMethod]
	public void When_Not_Building_Inside_Visual_Studio_Then_No_Assembly_Is_Deleted()
	{
		var (other, current) = RunCleanup(insideVisualStudio: false, disable: "");

		other.Should().BeTrue();
		current.Should().BeTrue();
	}

	/// <returns>Whether the head's assembly is still in the intermediate folder of the other and the current target framework.</returns>
	private static (bool Other, bool Current) RunCleanup(bool insideVisualStudio, string disable)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-hr-cleanup-").FullName;
		try
		{
			// The paths and AssemblyName are set by the .NET SDK in an app.
			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>{TargetFramework}</TargetFramework>
						<OutputType>Exe</OutputType>
						<AssemblyName>Project</AssemblyName>
						<BaseIntermediateOutputPath>$(MSBuildProjectDirectory)\obj\</BaseIntermediateOutputPath>
						<IntermediateOutputPath>$(BaseIntermediateOutputPath)Debug\{TargetFramework}\</IntermediateOutputPath>
					</PropertyGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets")}" />
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Build.targets")}" />
				</Project>
				""");

			var otherAssembly = CreateAssembly(directory, OtherTargetFramework);
			var currentAssembly = CreateAssembly(directory, TargetFramework);

			List<string> arguments = [projectPath, "-t:_UnoVSCleanupOtherTFMsForHR", "-p:IsImplicitlyTriggeredBuild=true"];
			if (insideVisualStudio)
			{
				arguments.Add("-p:BuildingInsideVisualStudio=true");
			}
			if (disable.Length > 0)
			{
				arguments.Add($"-p:UnoDisableVSCleanupOtherTFMsForHR={disable}");
			}

			// From the repository root, so its global.json picks the SDK.
			var result = DotnetMSBuild.Run(RepositoryPaths.Root, arguments);
			result.ExitCode.Should().Be(0, result.Output);

			return (File.Exists(otherAssembly), File.Exists(currentAssembly));
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}

	private static string CreateAssembly(string projectDirectory, string targetFramework)
	{
		var folder = Directory.CreateDirectory(Path.Join(projectDirectory, "obj", "Debug", targetFramework)).FullName;
		var path = Path.Join(folder, "Project.dll");
		File.WriteAllBytes(path, []);
		return path;
	}
}

using System.Diagnostics;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// The add-ins' buildTransitive targets reach every consumer (Svg is referenced implicitly by the Uno.Sdk), yet
/// nothing in the repository imports them, so a condition MSBuild cannot parse only surfaces in app builds.
/// </summary>
[TestClass]
public class Given_AddInBuildTransitiveTargets
{
	private static readonly string[] TargetFrameworks = ["net10.0", "net10.0-desktop", "net10.0-android", "net10.0-browserwasm", "net10.0-ios"];

	public static IEnumerable<object[]> ImportsPerTargetFramework =>
		Directory.EnumerateFiles(RepositoryPaths.Get("src", "AddIns"), "*.targets", SearchOption.AllDirectories)
			.Where(path => Path.GetFileName(Path.GetDirectoryName(path)) == "buildTransitive")
			.SelectMany(path => TargetFrameworks.Select(tfm => new object[] { Path.GetRelativePath(RepositoryPaths.Root, path), tfm }));

	[TestMethod]
	[DynamicData(nameof(ImportsPerTargetFramework))]
	public void When_Imported_By_A_Head_Then_The_Targets_Evaluate(string targets, string targetFramework)
	{
		var (exitCode, output) = Evaluate(targets, targetFramework, "TargetFramework");

		exitCode.Should().Be(0, output);
	}

	[TestMethod]
	[DataRow("Svg", "net10.0", "true")]
	[DataRow("Svg", "net10.0-desktop", "true")]
	[DataRow("Svg", "net10.0-ios", "false")]
	public void When_Head_Has_A_Runtime_Host_Then_The_Skia_Check_Applies_To_Desktop_Only(string addIn, string targetFramework, string expected)
	{
		var targets = Path.Combine("src", "AddIns", $"Uno.UI.{addIn}", "buildTransitive", $"Uno.WinUI.{addIn}.targets");

		var (exitCode, output) = Evaluate(targets, targetFramework, $"_Uno{addIn}CheckApplies");

		exitCode.Should().Be(0, output);
		output.Trim().Should().Be(expected);
	}

	private (int ExitCode, string Output) Evaluate(string targets, string targetFramework, string property)
	{
		var directory = Path.Combine(Path.GetTempPath(), "uno-addin-targets", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var projectPath = Path.Combine(directory, "Head.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>{targetFramework}</TargetFramework>
						<UnoHasRuntimeHost>true</UnoHasRuntimeHost>
						<IsUnoHead>true</IsUnoHead>
					</PropertyGroup>
					<Import Project="{RepositoryPaths.Get(targets)}" />
				</Project>
				""");

			var startInfo = new ProcessStartInfo("dotnet")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				WorkingDirectory = directory,
			};
			foreach (var argument in new[] { "msbuild", projectPath, "-nologo", "-nodeReuse:false", $"-getProperty:{property}" })
			{
				startInfo.ArgumentList.Add(argument);
			}
			startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

			using var process = Process.Start(startInfo)!;
			var stdout = process.StandardOutput.ReadToEndAsync();
			var stderr = process.StandardError.ReadToEndAsync();
			process.WaitForExit();

			return (process.ExitCode, stdout.Result + stderr.Result);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}
}

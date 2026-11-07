using System.Diagnostics;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// UnoDefaultFont=OpenSans makes the source generators emit a module initializer that sets the default text font of
/// the whole app, so Uno.Sdk must only default it for app heads: a library would override the app's own choice as
/// soon as its assembly loads (#25635).
/// </summary>
[TestClass]
public class Given_UnoDefaultFont
{
	[TestMethod]
	[DataRow("net10.0-desktop")]
	[DataRow("net10.0-browserwasm")]
	[DataRow("net10.0-android")]
	[DataRow("net10.0-ios")]
	public void When_App_Then_OpenSans_Is_The_Default(string targetFramework)
	{
		Evaluate(targetFramework, "Exe").Should().Be("OpenSans");
	}

	[TestMethod]
	[DataRow("net10.0-desktop")]
	[DataRow("net10.0-browserwasm")]
	[DataRow("net10.0-android")]
	[DataRow("net10.0-ios")]
	public void When_Library_Then_No_Default_Font_Is_Set(string targetFramework)
	{
		Evaluate(targetFramework, "Library").Should().BeEmpty();
	}

	[TestMethod]
	public void When_Library_Sets_OpenSans_Then_It_Is_Kept()
	{
		Evaluate("net10.0-desktop", "Library", unoDefaultFont: "OpenSans").Should().Be("OpenSans");
	}

	private static string Evaluate(string targetFramework, string outputType, string unoDefaultFont = "")
	{
		var directory = Path.Combine(Path.GetTempPath(), "uno-default-font", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var projectPath = Path.Combine(directory, "Head.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>{targetFramework}</TargetFramework>
						<OutputType>{outputType}</OutputType>
						<UnoDefaultFont>{unoDefaultFont}</UnoDefaultFont>
					</PropertyGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets")}" />
				</Project>
				""");

			var startInfo = new ProcessStartInfo("dotnet")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				WorkingDirectory = directory,
			};
			foreach (var argument in new[] { "msbuild", projectPath, "-nologo", "-nodeReuse:false", "-getProperty:UnoDefaultFont" })
			{
				startInfo.ArgumentList.Add(argument);
			}
			startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

			using var process = Process.Start(startInfo)!;
			var stdout = process.StandardOutput.ReadToEndAsync();
			var stderr = process.StandardError.ReadToEndAsync();
			process.WaitForExit();

			process.ExitCode.Should().Be(0, stdout.Result + stderr.Result);
			return stdout.Result.Trim();
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}
}

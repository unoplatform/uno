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

			var result = DotnetMSBuild.Run(RepositoryPaths.Root, [projectPath, "-getProperty:UnoDefaultFont"]);

			result.ExitCode.Should().Be(0, result.Output);
			return result.StandardOutput.Trim();
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}

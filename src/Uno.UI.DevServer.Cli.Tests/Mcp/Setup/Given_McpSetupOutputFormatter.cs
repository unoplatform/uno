using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_McpSetupOutputFormatter
{
	[TestMethod]
	public void ShortenPath_WorkspaceRelative_ReturnsDotPath()
	{
		var result = McpSetupOutputFormatter.ShortenPath(
			"/project/.cursor/mcp.json",
			"/project",
			"/home/testuser",
			isWindows: false);

		result.Should().Be($".{Path.DirectorySeparatorChar}.cursor{Path.DirectorySeparatorChar}mcp.json");
	}

	[TestMethod]
	public void ShortenPath_HomeRelative_ReturnsTildePath()
	{
		var result = McpSetupOutputFormatter.ShortenPath(
			"/home/testuser/.cursor/mcp.json",
			"/project",
			"/home/testuser",
			isWindows: false);

		result.Should().Be($"~{Path.DirectorySeparatorChar}.cursor{Path.DirectorySeparatorChar}mcp.json");
	}

	[TestMethod]
	public void ShortenPath_LinuxCaseMismatch_DoesNotShorten()
	{
		var result = McpSetupOutputFormatter.ShortenPath(
			"/Project/.cursor/mcp.json",
			"/project",
			"/home/testuser",
			isWindows: false);

		result.Should().Be($"{Path.DirectorySeparatorChar}Project{Path.DirectorySeparatorChar}.cursor{Path.DirectorySeparatorChar}mcp.json");
	}
}

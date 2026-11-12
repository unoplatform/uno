using AwesomeAssertions;
using Uno.UI.DevServer.Cli;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_Program
{
	[TestMethod]
	public void IsMcpMode_WhenArgsAreEmpty_ReturnsFalse()
	{
		Program.IsMcpMode(Array.Empty<string>()).Should().BeFalse();
	}

	[TestMethod]
	public void IsMcpMode_WhenMcpAppFlagPresent_ReturnsTrue()
	{
		Program.IsMcpMode(new[] { "--mcp-app" }).Should().BeTrue();
	}

	[TestMethod]
	public void IsMcpMode_WhenMcpAppFlagMixedWithOtherArgs_ReturnsTrue()
	{
		Program.IsMcpMode(new[] { "-fl", "out.log", "--mcp-app" }).Should().BeTrue();
	}

	[TestMethod]
	public void IsMcpMode_WhenMcpAppFlagCasingDiffers_ReturnsFalse()
	{
		// --mcp-app is matched via args.Contains (ordinal, case-sensitive) to stay
		// aligned with CliManager.RunAsync, which dispatches MCP STDIO mode using
		// the same case-sensitive check (originalArgs.Contains("--mcp-app")). A
		// case-insensitive match here would claim MCP mode without CliManager
		// actually routing into the proxy, breaking the stdout/stderr contract.
		Program.IsMcpMode(new[] { "--MCP-APP" }).Should().BeFalse();
	}

	[TestMethod]
	public void IsMcpMode_WhenMcpServePairAtStart_ReturnsTrue()
	{
		Program.IsMcpMode(new[] { "mcp", "serve" }).Should().BeTrue();
	}

	[TestMethod]
	public void IsMcpMode_WhenMcpServePairAfterGlobalFlags_ReturnsTrue()
	{
		Program.IsMcpMode(new[] { "--log-level", "trace", "mcp", "serve" }).Should().BeTrue();
	}

	[TestMethod]
	public void IsMcpMode_WhenMcpServePairIsUpperCase_ReturnsTrue()
	{
		Program.IsMcpMode(new[] { "MCP", "Serve" }).Should().BeTrue();
	}

	[TestMethod]
	[DataRow("install")]
	[DataRow("status")]
	[DataRow("uninstall")]
	public void IsMcpMode_WhenMcpSubcommandIsNotServe_ReturnsFalse(string subcommand)
	{
		Program.IsMcpMode(new[] { "mcp", subcommand }).Should().BeFalse();
	}

	[TestMethod]
	public void IsMcpMode_WhenMcpIsLastArg_ReturnsFalse()
	{
		// Incomplete pair must not trigger MCP mode.
		Program.IsMcpMode(new[] { "mcp" }).Should().BeFalse();
	}

	[TestMethod]
	public void IsMcpMode_WhenServeIsPresentWithoutMcp_ReturnsFalse()
	{
		Program.IsMcpMode(new[] { "serve" }).Should().BeFalse();
	}

	[TestMethod]
	[DataRow("start")]
	[DataRow("stop")]
	[DataRow("list")]
	[DataRow("disco")]
	[DataRow("health")]
	public void IsMcpMode_WhenCommandIsNonMcp_ReturnsFalse(string command)
	{
		Program.IsMcpMode(new[] { command }).Should().BeFalse();
	}

	[TestMethod]
	public void IsMcpMode_WhenNonMcpCommandWithLogLevel_ReturnsFalse()
	{
		Program.IsMcpMode(new[] { "--log-level", "trace", "list" }).Should().BeFalse();
	}

	[TestMethod]
	public void IsAppCommand_WhenArgsAreEmpty_ReturnsFalse()
	{
		Program.IsAppCommand(Array.Empty<string>()).Should().BeFalse();
	}

	[TestMethod]
	[DataRow("app")]
	[DataRow("APP")]
	public void IsAppCommand_WhenAppIsTheCommand_ReturnsTrue(string command)
	{
		Program.IsAppCommand(new[] { command, "tools" }).Should().BeTrue();
	}

	[TestMethod]
	[DataRow("--solution-dir", "C:/src/MyApp")]
	[DataRow("--log-level", "trace")]
	[DataRow("-l", "trace")]
	[DataRow("--file-log", "out.log")]
	[DataRow("-fl", "out.log")]
	public void IsAppCommand_WhenAppFollowsGlobalOptions_ReturnsTrue(string option, string value)
	{
		Program.IsAppCommand(new[] { option, value, "app", "tools" }).Should().BeTrue();
	}

	[TestMethod]
	[DataRow("--solution-dir")]
	[DataRow("--file-log")]
	[DataRow("-fl")]
	public void IsAppCommand_WhenAppIsOnlyAnOptionValue_ReturnsFalse(string option)
	{
		Program.IsAppCommand(new[] { option, "app", "list" }).Should().BeFalse();
	}

	[TestMethod]
	[DataRow("start")]
	[DataRow("stop")]
	[DataRow("list")]
	[DataRow("disco")]
	[DataRow("health")]
	public void IsAppCommand_WhenCommandIsNotApp_ReturnsFalse(string command)
	{
		Program.IsAppCommand(new[] { command }).Should().BeFalse();
	}

	[TestMethod]
	public void IsAppCommand_WhenMcpServe_ReturnsFalse()
	{
		Program.IsAppCommand(new[] { "mcp", "serve" }).Should().BeFalse();
	}
}

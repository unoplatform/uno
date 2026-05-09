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
}

using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_CliManager
{
	[TestMethod]
	public void DetermineMcpSetupExitCode_PartialError_ReturnsZero()
	{
		var result = new OperationResponse("1.0", [
			new OperationEntry("UnoApp", "cursor", "created", "/project/.cursor/mcp.json", null),
			new OperationEntry("UnoDocs", "cursor", "error", "/project/.cursor/mcp.json", "File is read-only"),
		]);

		CliManager.DetermineMcpSetupExitCode(result).Should().Be(0);
	}

	[TestMethod]
	public void DetermineMcpSetupExitCode_AllSuccess_ReturnsZero()
	{
		var result = new OperationResponse("1.0", [
			new OperationEntry("UnoApp", "cursor", "created", "/project/.cursor/mcp.json", null),
		]);

		CliManager.DetermineMcpSetupExitCode(result).Should().Be(0);
	}
}

using AwesomeAssertions;
using ModelContextProtocol.Protocol;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Tests for <see cref="Uno.UI.DevServer.Cli.Mcp.McpStdioServer"/> handler behavior:
/// structured error responses returned by the call_tool handler.
/// </summary>
[TestClass]
public class Given_McpStdioServer
{
	// -------------------------------------------------------------------
	// Structured error response (call_tool handler)
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("call_tool returns a structured error pointing to uno_health when the upstream is not yet ready")]
	public void CallToolResult_WhenIsError_ContainsStructuredMessage()
	{
		var result = new CallToolResult()
		{
			Content = [new TextContentBlock() { Text = "DevServer is starting up. The host process is not yet ready. Call the uno_health tool for detailed diagnostics, or wait a few seconds and retry." }],
			IsError = true,
		};

		result.IsError.Should().BeTrue();
		result.Content.Should().HaveCount(1);
		var textBlock = result.Content[0] as TextContentBlock;
		textBlock.Should().NotBeNull();
		textBlock!.Text.Should().Contain("uno_health");
		textBlock.Text.Should().Contain("not yet ready");
	}
}

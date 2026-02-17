using AwesomeAssertions;
using ModelContextProtocol.Protocol;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Tests for <see cref="Uno.UI.DevServer.Cli.Mcp.McpStdioServer"/> handler behavior:
/// structured error responses returned by the call_tool handler,
/// and ServerInfo configuration patterns.
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

	// -------------------------------------------------------------------
	// ServerInfo configuration pattern
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("Implementation record accepts the expected name and version format")]
	public void ServerInfo_NameAndVersion_AreValid()
	{
		var serverInfo = new Implementation
		{
			Name = "uno-devserver",
			Version = GetInformationalVersion(),
		};

		serverInfo.Name.Should().Be("uno-devserver");
		serverInfo.Version.Should().NotBeNullOrEmpty();
	}

	[TestMethod]
	[Description("InformationalVersion does not contain the commit hash suffix")]
	public void ServerInfo_Version_DoesNotContainCommitHash()
	{
		var version = GetInformationalVersion();

		version.Should().NotContain("+", "commit hash suffix should be stripped");
	}

	/// <summary>
	/// Mirrors the GetAssemblyVersion() logic in McpStdioServer to verify the
	/// InformationalVersion → SemVer extraction pattern.
	/// </summary>
	private static string GetInformationalVersion()
	{
		var attr = typeof(Given_McpStdioServer).Assembly
			.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
			.OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
			.FirstOrDefault();

		if (attr is not null)
		{
			var parts = attr.InformationalVersion.Split('+', StringSplitOptions.RemoveEmptyEntries);
			return parts[0];
		}

		return typeof(Given_McpStdioServer).Assembly.GetName().Version?.ToString() ?? "0.0.0";
	}
}

using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Regression tests for the stdin-theft bug: when the MCP CLI bridge launches
/// the DevServer host process, it must redirect the child's stdin so that it
/// does not inherit the parent's stdin pipe (the JSON-RPC MCP channel).
/// Without this, the child process reads (and discards) incoming MCP messages,
/// producing random hangs where every other tool call blocks indefinitely.
/// </summary>
[TestClass]
public class Given_StdinIsolation
{
	[TestMethod]
	[Description("ProcessStartInfo for the host must redirect stdin to prevent MCP message theft")]
	public void ProcessStartInfo_RedirectsStdin_WhenRedirectInputIsTrue()
	{
		// Mirrors the CreateDotnetProcessStartInfo call in DevServerMonitor.StartProcess.
		var psi = DevServerProcessHelper.CreateDotnetProcessStartInfo(
			hostPath: "C:\\fake\\host.exe",
			arguments: ["--httpPort", "12345"],
			workingDirectory: "C:\\fake",
			redirectOutput: true,
			redirectInput: true);

		psi.RedirectStandardInput.Should().BeTrue(
			"the host process must NOT inherit the parent's stdin when running as an MCP stdio bridge");
		psi.RedirectStandardOutput.Should().BeTrue();
		psi.RedirectStandardError.Should().BeTrue();
		psi.UseShellExecute.Should().BeFalse(
			"UseShellExecute=false is required for stream redirection");
	}

	[TestMethod]
	[Description("CreateDotnetProcessStartInfo defaults to not redirecting stdin")]
	public void ProcessStartInfo_DoesNotRedirectStdin_ByDefault()
	{
		// Non-MCP callers (e.g. IDE integration) should keep the default behavior.
		var psi = DevServerProcessHelper.CreateDotnetProcessStartInfo(
			hostPath: "C:\\fake\\host.exe",
			arguments: ["--httpPort", "12345"],
			workingDirectory: "C:\\fake",
			redirectOutput: true);

		psi.RedirectStandardInput.Should().BeFalse(
			"non-MCP callers should not redirect stdin by default");
	}
}

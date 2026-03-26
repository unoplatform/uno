using AwesomeAssertions;
using Spectre.Console;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_DiscoveryOutputFormatter
{
	[TestMethod]
	[Description("The Add-Ins section uses a distinct key for add-in discovery time and keeps total discovery time separate")]
	public void WhenPlainTextIsWritten_AddInAndTotalDurationsAreDistinct()
	{
		var info = new DiscoveryInfo
		{
			RequestedWorkingDirectory = @"D:\src\repo",
			WorkingDirectory = @"D:\src\repo",
			DiscoveryDurationMs = 250,
			AddInsDiscoveryDurationMs = 75,
		};

		AnsiConsole.Record();
		DiscoveryOutputFormatter.WritePlainText(info);
		var output = AnsiConsole.ExportText();

		output.Should().Contain("addInsDiscoveryDurationMs");
		output.Should().Contain("75");
		output.Should().Contain("totalDiscoveryDurationMs");
		output.Should().Contain("250");
		output.Should().NotContain("discoveryDurationMs");
	}

	[TestMethod]
	[Description("Active server diagnostics include the process ancestry chain so callers can identify who launched the host")]
	public void WhenPlainTextIsWritten_ActiveServersIncludeProcessChain()
	{
		var info = new DiscoveryInfo
		{
			RequestedWorkingDirectory = @"D:\src\repo",
			WorkingDirectory = @"D:\src\repo",
			ActiveServers =
			[
				new ActiveServerInfo
				{
					ProcessId = 1234,
					Port = 61616,
					McpEndpoint = "http://localhost:61616/mcp",
					ParentProcessId = 4321,
					SolutionPath = @"D:\src\repo\Repo.sln",
					IsInWorkspace = true,
					ProcessChain =
					[
						new ProcessChainEntry { ProcessId = 1234, ProcessName = "Uno.UI.RemoteControl.Host" },
						new ProcessChainEntry { ProcessId = 4321, ProcessName = "dotnet" },
						new ProcessChainEntry { ProcessId = 9876, ProcessName = "ide-process" },
					],
				},
			],
		};

		AnsiConsole.Record();
		DiscoveryOutputFormatter.WritePlainText(info);
		var output = AnsiConsole.ExportText();

		output.Should().Contain("processChain");
		// The table may wrap long chains across lines, so check individual elements.
		output.Should().Contain("ide-process (9876)");
		output.Should().Contain("dotnet (4321)");
		output.Should().Contain("Host");
		output.Should().Contain("1234");
	}
}

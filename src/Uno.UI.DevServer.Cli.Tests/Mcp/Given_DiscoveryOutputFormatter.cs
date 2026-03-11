using AwesomeAssertions;
using Spectre.Console;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_DiscoveryOutputFormatter
{
	[TestMethod]
	[Description("The Add-Ins section shows add-in discovery time separately from total discovery time")]
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

		output.Should().Contain("discoveryDurationMs");
		output.Should().Contain("75");
		output.Should().Contain("totalDiscoveryDurationMs");
		output.Should().Contain("250");
	}
}

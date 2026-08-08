using AwesomeAssertions;
using Uno.UI.DevServer.Cli;

namespace Uno.UI.DevServer.Cli.Tests;

[TestClass]
public class Given_SettingsExitReason
{
	[TestMethod]
	public void WhenOnlyStdout_LabelsStdout()
		=> CliManager.FormatSettingsExitReason("Another instance is running", null)
			.Should().Be("stdout: Another instance is running");

	[TestMethod]
	public void WhenOnlyStderr_LabelsStderr()
		=> CliManager.FormatSettingsExitReason(null, "boom")
			.Should().Be("stderr: boom");

	[TestMethod]
	public void WhenBothStreams_StdoutFirstSemicolonSeparated()
		=> CliManager.FormatSettingsExitReason("out", "err")
			.Should().Be("stdout: out; stderr: err");

	[TestMethod]
	[DataRow(null, null)]
	[DataRow("", "   ")]
	public void WhenNoOutput_ReturnsEmpty(string? stdOut, string? stdErr)
		=> CliManager.FormatSettingsExitReason(stdOut, stdErr).Should().BeEmpty();

	[TestMethod]
	public void WhenMultilineOutput_CollapsedToSingleLine()
	{
		var result = CliManager.FormatSettingsExitReason("line1\r\nline2\n  line3  ", null);

		result.Should().Be("stdout: line1 line2 line3");
	}
}

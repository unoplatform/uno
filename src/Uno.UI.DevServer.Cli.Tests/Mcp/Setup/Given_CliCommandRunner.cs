using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_CliCommandRunner
{
	// ──────────────────────────────────────────────
	// ExpandArgs
	// ──────────────────────────────────────────────

	[TestMethod]
	public void ExpandArgs_SubstitutesName()
	{
		var template = new[] { "mcp", "add", "{name}" };
		var placeholders = new Dictionary<string, object> { ["name"] = "UnoApp" };

		var result = CliCommandRunner.ExpandArgs(template, placeholders);

		result.Should().BeEquivalentTo(new[] { "mcp", "add", "UnoApp" });
	}

	[TestMethod]
	public void ExpandArgs_SubstitutesCommandAndArgs()
	{
		var template = new[] { "mcp", "add", "{name}", "--", "{command}", "{args...}" };
		var placeholders = new Dictionary<string, object>
		{
			["name"] = "UnoApp",
			["command"] = "dnx",
			["args"] = new[] { "-y", "uno.devserver", "--mcp-app" },
		};

		var result = CliCommandRunner.ExpandArgs(template, placeholders);

		result.Should().BeEquivalentTo(new[]
		{
			"mcp", "add", "UnoApp", "--", "dnx", "-y", "uno.devserver", "--mcp-app"
		});
	}

	[TestMethod]
	public void ExpandArgs_SubstitutesUrl()
	{
		var template = new[] { "mcp", "add", "--transport", "http", "{name}", "{url}" };
		var placeholders = new Dictionary<string, object>
		{
			["name"] = "UnoDocs",
			["url"] = "https://mcp.platform.uno/v1",
		};

		var result = CliCommandRunner.ExpandArgs(template, placeholders);

		result.Should().BeEquivalentTo(new[]
		{
			"mcp", "add", "--transport", "http", "UnoDocs", "https://mcp.platform.uno/v1"
		});
	}

	[TestMethod]
	public void ExpandArgs_LeavesUnresolvedPlaceholders()
	{
		var template = new[] { "mcp", "{unknown}" };
		var placeholders = new Dictionary<string, object>();

		var result = CliCommandRunner.ExpandArgs(template, placeholders);

		result.Should().BeEquivalentTo(new[] { "mcp", "{unknown}" });
	}

	[TestMethod]
	public void ExpandArgs_EmptyArgsArray_ProducesNothing()
	{
		var template = new[] { "mcp", "add", "{name}", "{args...}" };
		var placeholders = new Dictionary<string, object> { ["name"] = "UnoApp" };

		var result = CliCommandRunner.ExpandArgs(template, placeholders);

		result.Should().BeEquivalentTo(new[] { "mcp", "add", "UnoApp" });
	}

	[TestMethod]
	public void ExpandArgs_LiteralsArePreserved()
	{
		var template = new[] { "--scope", "project", "--transport", "stdio" };
		var placeholders = new Dictionary<string, object>();

		var result = CliCommandRunner.ExpandArgs(template, placeholders);

		result.Should().BeEquivalentTo(template);
	}

	// ──────────────────────────────────────────────
	// IsAvailable
	// ──────────────────────────────────────────────

	[TestMethod]
	public void IsAvailable_WithDotnet_ReturnsTrue()
	{
		var runner = new CliCommandRunner(
			Microsoft.Extensions.Logging.Abstractions.NullLogger<CliCommandRunner>.Instance);
		var profile = new CliProfile("dotnet", ["--version"], null, null, null, null);

		runner.IsAvailable(profile).Should().BeTrue();
	}

	[TestMethod]
	public void IsAvailable_WithNonexistent_ReturnsFalse()
	{
		var runner = new CliCommandRunner(
			Microsoft.Extensions.Logging.Abstractions.NullLogger<CliCommandRunner>.Instance);
		var profile = new CliProfile("nonexistent-tool-xyzzy-12345", ["--version"], null, null, null, null);

		runner.IsAvailable(profile).Should().BeFalse();
	}
}

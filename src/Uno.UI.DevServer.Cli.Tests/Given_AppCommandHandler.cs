using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Protocol;

namespace Uno.UI.DevServer.Cli.Tests;

[TestClass]
public class Given_AppCommandHandler
{
	[TestMethod]
	[Description("A verb maps to its uno_app_* tool; --name value pairs become typed tool arguments")]
	public void WhenVerbAndParams_ToolAndTypedArgumentsAreParsed()
	{
		var parsed = AppCommandHandler.Parse(["find", "--automationId", "Save", "--waitForMs", "5000", "--includeHidden", "--json"]);

		parsed.Error.Should().BeNull();
		parsed.ToolName.Should().Be("uno_app_find_elements");
		parsed.Json.Should().BeTrue();
		parsed.Arguments["automationId"].Should().Be("Save");
		((JsonElement)parsed.Arguments["waitForMs"]!).GetInt32().Should().Be(5000);
		parsed.Arguments["includeHidden"].Should().Be(true);
	}

	[TestMethod]
	[Description("An unknown verb is sent to the host as the tool name, so dynamic app_* tools work too")]
	public void WhenUnknownVerb_ItIsTheToolName()
	{
		var parsed = AppCommandHandler.Parse(["app_reset_cart", "--out", "x.png"]);

		parsed.Error.Should().BeNull();
		parsed.ToolName.Should().Be("app_reset_cart");
		parsed.OutputFile.Should().Be("x.png");
		parsed.Arguments.Should().BeEmpty();
	}

	[TestMethod]
	[Description("A positional argument after the verb is an error, not a silently dropped parameter")]
	public void WhenPositionalArgument_ErrorIsReported()
	{
		AppCommandHandler.Parse(["find", "Save"]).Error.Should().Contain("Save");
		AppCommandHandler.Parse([]).Error.Should().NotBeNull();
	}

	[TestMethod]
	[Description("JSON-looking values are typed, text that only starts like JSON stays a string")]
	public void WhenValueParsed_JsonOrString()
	{
		((JsonElement)AppCommandHandler.ParseValue("[1,2]")!).GetArrayLength().Should().Be(2);
		((JsonElement)AppCommandHandler.ParseValue("false")!).GetBoolean().Should().BeFalse();
		AppCommandHandler.ParseValue("2024-01-01").Should().Be("2024-01-01");
		AppCommandHandler.ParseValue("Save").Should().Be("Save");
	}

	[TestMethod]
	[Description("Exit code 1 for an error result or a find that matched nothing, 0 otherwise")]
	public void WhenResult_ExitCodeFollowsIt()
	{
		static CallToolResult Text(string text, bool? isError = null)
			=> new() { Content = [new TextContentBlock { Text = text }], IsError = isError };

		AppCommandHandler.ExitCodeFor("uno_app_find_elements", Text("1 matched automationId 'Save'")).Should().Be(0);
		AppCommandHandler.ExitCodeFor("uno_app_find_elements", Text("0 matched automationId 'Save' (waited 5000 ms)")).Should().Be(1);
		AppCommandHandler.ExitCodeFor("uno_app_get_runtime_info", Text("0 matched is just text here")).Should().Be(0);
		AppCommandHandler.ExitCodeFor("uno_app_start", Text("No app connected", isError: true)).Should().Be(1);
	}
}

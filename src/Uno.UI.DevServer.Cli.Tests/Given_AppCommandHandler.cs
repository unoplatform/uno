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
		AppCommandHandler.Parse(["hot-reload-status", "--file", "a.xaml"]).ToolName.Should().Be("uno_app_hot_reload_status");
		parsed.Json.Should().BeTrue();
		parsed.Arguments["automationId"].Should().Be("Save");
		((JsonElement)parsed.Arguments["waitForMs"]!).GetInt32().Should().Be(5000);
		parsed.Arguments["includeHidden"].Should().Be(true);
	}

	[TestMethod]
	[Description("A value that parsed as JSON goes back to text when the tool declares the parameter as a (nullable) string: --elementRef 1")]
	public void WhenToolDeclaresAString_ATypedValueIsSentAsText()
	{
		var schema = JsonDocument.Parse("""{"type":"object","properties":{"elementRef":{"type":["string","null"]},"name":{"type":"string"},"waitForMs":{"type":"integer"}}}""").RootElement;
		var arguments = new Dictionary<string, object?>(AppCommandHandler.Parse(["invoke", "--elementRef", "1", "--name", "42", "--waitForMs", "5000"]).Arguments);

		AppCommandHandler.CoerceStrings(schema, arguments);

		arguments["elementRef"].Should().Be("1");
		arguments["name"].Should().Be("42");
		((JsonElement)arguments["waitForMs"]!).GetInt32().Should().Be(5000);
	}

	[TestMethod]
	[Description("An unknown verb is sent to the host as the tool name, so dynamic app_* tools work too")]
	public void WhenUnknownVerb_ItIsTheToolName()
	{
		var parsed = AppCommandHandler.Parse(["app_reset_cart", "--out", "x.png", "--no-start"]);

		parsed.Error.Should().BeNull();
		parsed.ToolName.Should().Be("app_reset_cart");
		parsed.OutputFile.Should().Be("x.png");
		parsed.NoStart.Should().BeTrue();
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

	[TestMethod]
	[Description("A hot-reload status for a file is 0 when applied, 1 when not, 3 when a rude edit needs a start, 0 without a file verdict")]
	public void WhenHotReloadStatus_ExitCodeFollowsTheFileVerdict()
	{
		static CallToolResult Status(string verdict)
			=> new() { Content = [new TextContentBlock { Text = $"Hot Reload: Ready (2 operations)\nedit to 'C:\\app\\MainPage.xaml': {verdict}\n" }] };

		const string tool = "uno_app_hot_reload_status";
		AppCommandHandler.ExitCodeFor(tool, Status("applied (compiled by the server in 1.2 s, local op #3 applied 0.4 s later)")).Should().Be(0);
		AppCommandHandler.ExitCodeFor(tool, Status("NOT applied — server op #4 Failed in 1.1 s (fix the errors below and save again).\n  CS1002: ; expected")).Should().Be(1);
		AppCommandHandler.ExitCodeFor(tool, Status("no Hot Reload operation covered it within 10 s. The file was last written 11 s ago.")).Should().Be(1);
		AppCommandHandler.ExitCodeFor(tool, Status("NOT applied — server op #5 RudeEdit in 0.9 s (a rude edit cannot be hot-reloaded: restart with uno_app_start).")).Should().Be(AppCommandHandler.RestartNeededExitCode);
		AppCommandHandler.ExitCodeFor(tool, new CallToolResult { Content = [new TextContentBlock { Text = "Hot Reload: Ready (0 operations)" }] }).Should().Be(0);
	}

	[TestMethod]
	[Description("structuredContent, when the host sends it, decides the exit code before any text is read")]
	public void WhenStructuredContent_ExitCodeReadsIt()
	{
		static CallToolResult With(string json, string text = "prose that would say otherwise")
			=> new() { Content = [new TextContentBlock { Text = text }], StructuredContent = JsonSerializer.Deserialize<JsonElement>(json) };

		AppCommandHandler.ExitCodeFor("uno_app_find_elements", With("""{"matched":0,"elements":[]}""", "3 matched")).Should().Be(1);
		AppCommandHandler.ExitCodeFor("uno_app_find_elements", With("""{"matched":2,"elements":[]}""", "0 matched")).Should().Be(0);
		AppCommandHandler.ExitCodeFor("uno_app_hot_reload_status", With("""{"state":"Ready","verdict":"applied"}""")).Should().Be(0);
		AppCommandHandler.ExitCodeFor("uno_app_hot_reload_status", With("""{"state":"Ready","verdict":"notApplied","diagnostics":["x"]}""")).Should().Be(1);
		AppCommandHandler.ExitCodeFor("uno_app_hot_reload_status", With("""{"state":"Ready","verdict":"rudeEdit"}""")).Should().Be(AppCommandHandler.RestartNeededExitCode);
		AppCommandHandler.ExitCodeFor("uno_app_hot_reload_status", With("""{"state":"Ready"}""")).Should().Be(0);
	}

	[TestMethod]
	[Description("assert passes when every match carries the state tokens, a !token is absent, the count matches, or nothing matched with --absent")]
	public void WhenAssert_VerdictFollowsMatchesAndState()
	{
		const string two = "2 matched automationId 'Row'\nListViewItem ^3a #Row1 [s] {selected items=3}  \"A\"\nListViewItem ^3b #Row2 [s] {items=3}  \"B\"\n";
		const string none = "0 matched automationId 'Gone' (waited 2s). The match is exact.";

		AppCommandHandler.AssertFailure(two, [], false, null).Should().BeNull();
		AppCommandHandler.AssertFailure(two, ["items=3"], false, 2).Should().BeNull();
		AppCommandHandler.AssertFailure(two, ["selected"], false, null).Should().Contain("Row2");
		AppCommandHandler.AssertFailure(two, ["!selected"], false, null).Should().Contain("Row1");
		AppCommandHandler.AssertFailure(two, [], false, 1).Should().Contain("expected 1");
		AppCommandHandler.AssertFailure(two, [], true, null).Should().Contain("2 matched");
		AppCommandHandler.AssertFailure(none, [], true, null).Should().BeNull();
		AppCommandHandler.AssertFailure(none, [], false, null).Should().Contain("0 matched");
	}

	[TestMethod]
	[Description("--name resolves to the one ^ref whose #Name matches; none or several is an error naming the refs")]
	public void WhenNameResolved_RefComesFromTheSnapshot()
	{
		const string tree = "Page ^1\n  Button ^5k #Save [i] \"Save\"\n  Button ^5m #Cancel [i] \"Cancel\"\n  TextBlock ^5n #Save\n";

		AppCommandHandler.ResolveRefByName(tree, "Cancel", out var error).Should().Be("5m");
		error.Should().BeNull();
		AppCommandHandler.ResolveRefByName(tree, "Nope", out error).Should().BeNull();
		error.Should().Contain("Nope");
		AppCommandHandler.ResolveRefByName(tree, "Save", out error).Should().BeNull();
		error.Should().Contain("5k").And.Contain("5n");
	}
}

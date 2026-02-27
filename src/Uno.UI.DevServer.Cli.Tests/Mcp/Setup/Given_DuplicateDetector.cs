using System.Text.Json.Nodes;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_DuplicateDetector
{
	private static IReadOnlyDictionary<string, ServerDefinition> TestServers => new Dictionary<string, ServerDefinition>
	{
		["UnoApp"] = new(
			Transport: "stdio",
			Variants: new Dictionary<string, JsonObject>
			{
				["stable"] = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject(),
			},
			Detection: new(
				KeyPatterns: ["^UnoApp$"],
				CommandPatterns: [
					@"dnx.*uno\.devserver.*--mcp-app",
					@"uno\.devserver.*--mcp-app",
					@"dotnet\s+dnx.*uno\.devserver.*--mcp-app",
				],
				UrlPatterns: [
					@"localhost.*uno[._-]?devserver.*/mcp",
					@"localhost:\d+/mcp",
				])),
		["UnoDocs"] = new(
			Transport: "http",
			Variants: new Dictionary<string, JsonObject>
			{
				["stable"] = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject(),
			},
			Detection: new(
				KeyPatterns: ["^UnoDocs$"],
				CommandPatterns: null,
				UrlPatterns: [@"mcp\.platform\.uno"])),
	};

	// ── FindMatchingServer: key name match ──

	[TestMethod]
	public void FindMatchingServer_ExactKeyName_MatchesUnoApp()
	{
		var entry = JsonNode.Parse("""{"command":"something"}""")!.AsObject();
		var result = DuplicateDetector.FindMatchingServer("UnoApp", entry, TestServers);
		result.Should().Be("UnoApp");
	}

	[TestMethod]
	public void FindMatchingServer_ExactKeyName_MatchesUnoDocs()
	{
		var entry = JsonNode.Parse("""{"url":"https://example.com"}""")!.AsObject();
		var result = DuplicateDetector.FindMatchingServer("UnoDocs", entry, TestServers);
		result.Should().Be("UnoDocs");
	}

	[TestMethod]
	public void FindMatchingServer_UnknownKey_NoMatch()
	{
		var entry = JsonNode.Parse("""{"command":"other-tool","args":["--help"]}""")!.AsObject();
		var result = DuplicateDetector.FindMatchingServer("MyServer", entry, TestServers);
		result.Should().BeNull();
	}

	// ── FindMatchingServer: content match ──

	[TestMethod]
	public void FindMatchingServer_DnxCommandContent_MatchesUnoApp()
	{
		var entry = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var result = DuplicateDetector.FindMatchingServer("my-custom-name", entry, TestServers);
		result.Should().Be("UnoApp");
	}

	[TestMethod]
	public void FindMatchingServer_GlobalToolSyntax_MatchesUnoApp()
	{
		var entry = JsonNode.Parse("""{"command":"uno.devserver","args":["--mcp-app"]}""")!.AsObject();
		var result = DuplicateDetector.FindMatchingServer("uno", entry, TestServers);
		result.Should().Be("UnoApp");
	}

	[TestMethod]
	public void FindMatchingServer_DotnetDnxSyntax_MatchesUnoApp()
	{
		var entry = JsonNode.Parse("""{"command":"dotnet","args":["dnx","-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var result = DuplicateDetector.FindMatchingServer("devserver", entry, TestServers);
		result.Should().Be("UnoApp");
	}

	[TestMethod]
	public void FindMatchingServer_LegacyHttpUrl_MatchesUnoApp()
	{
		var entry = JsonNode.Parse("""{"url":"http://localhost:5000/mcp"}""")!.AsObject();
		var result = DuplicateDetector.FindMatchingServer("legacy-server", entry, TestServers);
		result.Should().Be("UnoApp");
	}

	[TestMethod]
	public void FindMatchingServer_UnoDocsUrl_MatchesUnoDocs()
	{
		var entry = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject();
		var result = DuplicateDetector.FindMatchingServer("docs", entry, TestServers);
		result.Should().Be("UnoDocs");
	}

	// ── DetectVariant ──

	[TestMethod]
	public void DetectVariant_StableArgs_ReturnsStable()
	{
		var entry = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var result = DuplicateDetector.DetectVariant(entry, TestServers["UnoApp"]);
		result.Should().Be("stable");
	}

	[TestMethod]
	public void DetectVariant_PrereleaseArgs_ReturnsPrerelease()
	{
		var entry = JsonNode.Parse("""{"command":"dnx","args":["-y","--prerelease","uno.devserver","--mcp-app"]}""")!.AsObject();
		var result = DuplicateDetector.DetectVariant(entry, TestServers["UnoApp"]);
		result.Should().Be("prerelease");
	}

	[TestMethod]
	public void DetectVariant_PinnedArgs_ReturnsPinnedWithVersion()
	{
		var entry = JsonNode.Parse("""{"command":"dnx","args":["-y","--version","5.6.0","uno.devserver","--mcp-app"]}""")!.AsObject();
		var result = DuplicateDetector.DetectVariant(entry, TestServers["UnoApp"]);
		result.Should().Be("pinned:5.6.0");
	}

	[TestMethod]
	public void DetectVariant_LegacyHttp_ReturnsLegacyHttp()
	{
		var entry = JsonNode.Parse("""{"url":"http://localhost:5000/mcp"}""")!.AsObject();
		var result = DuplicateDetector.DetectVariant(entry, TestServers["UnoApp"]);
		result.Should().Be("legacy-http");
	}

	// ── IsUpToDate ──

	[TestMethod]
	public void IsUpToDate_MatchingDefinition_ReturnsTrue()
	{
		var existing = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var expected = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var result = DuplicateDetector.IsUpToDate(existing, expected, TestServers["UnoApp"]);
		result.Should().BeTrue();
	}

	[TestMethod]
	public void IsUpToDate_DifferentArgs_ReturnsFalse()
	{
		var existing = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var expected = JsonNode.Parse("""{"command":"dnx","args":["-y","--prerelease","uno.devserver","--mcp-app"]}""")!.AsObject();
		var result = DuplicateDetector.IsUpToDate(existing, expected, TestServers["UnoApp"]);
		result.Should().BeFalse();
	}

	[TestMethod]
	public void IsUpToDate_ExtraKeys_StillReturnsTrue()
	{
		var existing = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"],"env":{"FOO":"bar"},"disabled":false}""")!.AsObject();
		var expected = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var result = DuplicateDetector.IsUpToDate(existing, expected, TestServers["UnoApp"]);
		result.Should().BeTrue();
	}

	[TestMethod]
	public void IsUpToDate_HttpTransport_MatchingUrl_ReturnsTrue()
	{
		var existing = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject();
		var expected = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject();
		var result = DuplicateDetector.IsUpToDate(existing, expected, TestServers["UnoDocs"]);
		result.Should().BeTrue();
	}

	[TestMethod]
	public void IsUpToDate_HttpTransport_DifferentUrl_ReturnsFalse()
	{
		var existing = JsonNode.Parse("""{"url":"https://old.mcp.platform.uno/v1"}""")!.AsObject();
		var expected = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject();
		var result = DuplicateDetector.IsUpToDate(existing, expected, TestServers["UnoDocs"]);
		result.Should().BeFalse();
	}
}

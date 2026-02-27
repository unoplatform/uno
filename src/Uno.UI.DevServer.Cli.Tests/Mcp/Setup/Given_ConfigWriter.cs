using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_ConfigWriter
{
	// ── MergeServer: create new file ──

	[TestMethod]
	public void MergeServer_NullContent_CreatesNewFile()
	{
		var def = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();

		var result = ConfigWriter.MergeServer(null, "mcpServers", "UnoApp", def, includeType: false, transport: null);

		var parsed = JsonNode.Parse(result)!.AsObject();
		var servers = parsed["mcpServers"]!.AsObject();
		servers["UnoApp"]!["command"]!.GetValue<string>().Should().Be("dnx");
	}

	[TestMethod]
	public void MergeServer_EmptyContent_CreatesNewFile()
	{
		var def = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();

		var result = ConfigWriter.MergeServer("", "mcpServers", "UnoApp", def, includeType: false, transport: null);

		var parsed = JsonNode.Parse(result)!.AsObject();
		parsed["mcpServers"].Should().NotBeNull();
	}

	// ── MergeServer: VS Code format with type ──

	[TestMethod]
	public void MergeServer_VsCodeFormat_IncludesType()
	{
		var def = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();

		var result = ConfigWriter.MergeServer(null, "servers", "UnoApp", def, includeType: true, transport: "stdio");

		var parsed = JsonNode.Parse(result)!.AsObject();
		var entry = parsed["servers"]!["UnoApp"]!.AsObject();
		entry["type"]!.GetValue<string>().Should().Be("stdio");
		entry["command"]!.GetValue<string>().Should().Be("dnx");
	}

	[TestMethod]
	public void MergeServer_HttpServer_VsCodeFormat_IncludesType()
	{
		var def = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject();

		var result = ConfigWriter.MergeServer(null, "servers", "UnoDocs", def, includeType: true, transport: "http");

		var parsed = JsonNode.Parse(result)!.AsObject();
		var entry = parsed["servers"]!["UnoDocs"]!.AsObject();
		entry["type"]!.GetValue<string>().Should().Be("http");
		entry["url"]!.GetValue<string>().Should().Be("https://mcp.platform.uno/v1");
	}

	// ── MergeServer: preserves other entries ──

	[TestMethod]
	public void MergeServer_PreservesOtherEntries()
	{
		var existing = """
		{
		  "mcpServers": {
		    "OtherServer": {"command": "other", "args": ["--run"]}
		  }
		}
		""";
		var def = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();

		var result = ConfigWriter.MergeServer(existing, "mcpServers", "UnoApp", def, includeType: false, transport: null);

		var parsed = JsonNode.Parse(result)!.AsObject();
		var servers = parsed["mcpServers"]!.AsObject();
		servers["OtherServer"].Should().NotBeNull();
		servers["UnoApp"].Should().NotBeNull();
	}

	// ── MergeServer: shallow merge preserves unknown keys ──

	[TestMethod]
	public void MergeServer_ShallowMerge_PreservesEnvAndDisabled()
	{
		var existing = """
		{
		  "mcpServers": {
		    "UnoApp": {
		      "command": "dnx",
		      "args": ["-y", "uno.devserver", "--mcp-app"],
		      "env": {"MY_VAR": "value"},
		      "disabled": false
		    }
		  }
		}
		""";
		var def = JsonNode.Parse("""{"command":"dnx","args":["-y","--prerelease","uno.devserver","--mcp-app"]}""")!.AsObject();

		var result = ConfigWriter.MergeServer(existing, "mcpServers", "UnoApp", def, includeType: false, transport: null);

		var parsed = JsonNode.Parse(result)!.AsObject();
		var entry = parsed["mcpServers"]!["UnoApp"]!.AsObject();
		entry["command"]!.GetValue<string>().Should().Be("dnx");
		entry["args"]![1]!.GetValue<string>().Should().Be("--prerelease");
		entry["env"]!["MY_VAR"]!.GetValue<string>().Should().Be("value");
		entry["disabled"]!.GetValue<bool>().Should().BeFalse();
	}

	// ── MergeServer: JSONC handling ──

	[TestMethod]
	public void MergeServer_JsoncContent_ParsedCorrectly()
	{
		var existing = """
		{
		  // This is a comment
		  "mcpServers": {
		    "OtherServer": {"command": "other"},
		  }
		}
		""";
		var def = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();

		var result = ConfigWriter.MergeServer(existing, "mcpServers", "UnoApp", def, includeType: false, transport: null);

		var parsed = JsonNode.Parse(result)!.AsObject();
		var servers = parsed["mcpServers"]!.AsObject();
		servers["OtherServer"].Should().NotBeNull();
		servers["UnoApp"].Should().NotBeNull();
	}

	// ── MergeServer: malformed JSON ──

	[TestMethod]
	public void MergeServer_MalformedJson_ThrowsJsonException()
	{
		var malformed = "{ this is not valid json }}}";
		var def = JsonNode.Parse("""{"command":"test"}""")!.AsObject();

		var act = () => ConfigWriter.MergeServer(malformed, "mcpServers", "UnoApp", def, includeType: false, transport: null);
		act.Should().Throw<JsonException>();
	}

	// ── MergeServer: formatting ──

	[TestMethod]
	public void MergeServer_Output_HasTrailingNewline()
	{
		var def = JsonNode.Parse("""{"command":"test"}""")!.AsObject();
		var result = ConfigWriter.MergeServer(null, "mcpServers", "Test", def, includeType: false, transport: null);
		result.Should().EndWith("\n");
	}

	[TestMethod]
	public void MergeServer_Output_IsIndented()
	{
		var def = JsonNode.Parse("""{"command":"test"}""")!.AsObject();
		var result = ConfigWriter.MergeServer(null, "mcpServers", "Test", def, includeType: false, transport: null);
		result.Should().Contain("  "); // indented
	}

	// ── RemoveServer ──

	[TestMethod]
	public void RemoveServer_ExistingEntry_RemovesIt()
	{
		var existing = """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]},
		    "OtherServer": {"command": "other"}
		  }
		}
		""";

		var result = ConfigWriter.RemoveServer(existing, "mcpServers", "UnoApp");

		result.Should().NotBeNull();
		var parsed = JsonNode.Parse(result!)!.AsObject();
		var servers = parsed["mcpServers"]!.AsObject();
		servers.ContainsKey("UnoApp").Should().BeFalse();
		servers["OtherServer"].Should().NotBeNull();
	}

	[TestMethod]
	public void RemoveServer_NotFound_ReturnsNull()
	{
		var existing = """
		{
		  "mcpServers": {
		    "OtherServer": {"command": "other"}
		  }
		}
		""";

		var result = ConfigWriter.RemoveServer(existing, "mcpServers", "UnoApp");
		result.Should().BeNull();
	}

	[TestMethod]
	public void RemoveServer_EmptyContent_ReturnsNull()
	{
		var result = ConfigWriter.RemoveServer(null, "mcpServers", "UnoApp");
		result.Should().BeNull();
	}

	[TestMethod]
	public void RemoveServer_NoRootKey_ReturnsNull()
	{
		var existing = """{"otherKey": {}}""";
		var result = ConfigWriter.RemoveServer(existing, "mcpServers", "UnoApp");
		result.Should().BeNull();
	}
}

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_ConfigScanner
{
	private static IReadOnlyDictionary<string, ServerDefinition> TestServers => new Dictionary<string, ServerDefinition>
	{
		["UnoApp"] = new(
			Transport: "stdio",
			Variants: new Dictionary<string, JsonObject>
			{
				["stable"] = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject(),
				["prerelease"] = JsonNode.Parse("""{"command":"dnx","args":["-y","--prerelease","uno.devserver","--mcp-app"]}""")!.AsObject(),
			},
			Detection: new(
				KeyPatterns: ["^UnoApp$"],
				CommandPatterns: [@"dnx.*uno\.devserver.*--mcp-app"],
				UrlPatterns: [@"localhost:\d+/mcp"])),
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

	private static IdeProfile CursorProfile => new(
		ConfigPaths: ["{workspace}/.cursor/mcp.json", "{home}/.cursor/mcp.json"],
		WriteTarget: "{workspace}/.cursor/mcp.json",
		JsonRootKey: "mcpServers");

	private static IdeProfile VsCodeProfile => new(
		ConfigPaths: ["{workspace}/.vscode/mcp.json", "{home}/.vscode/mcp.json"],
		WriteTarget: "{workspace}/.vscode/mcp.json",
		JsonRootKey: "servers");

	// ── Server registered ──

	[TestMethod]
	public void Scan_ServerRegistered_ReturnsRegistered()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var stableDef = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var expectedDefs = new Dictionary<string, JsonObject> { ["UnoApp"] = stableDef, ["UnoDocs"] = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject() };

		var scanner = new ConfigScanner(fs);
		var result = scanner.Scan(CursorProfile, "/project", TestServers, expectedDefs);

		result.ServerResults["UnoApp"].Status.Should().Be("registered");
		result.ServerResults["UnoApp"].Locations.Should().HaveCount(1);
	}

	// ── Server missing ──

	[TestMethod]
	public void Scan_NoConfigFiles_ReturnsMissing()
	{
		var fs = new InMemoryFileSystem();
		var stableDef = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var expectedDefs = new Dictionary<string, JsonObject> { ["UnoApp"] = stableDef, ["UnoDocs"] = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject() };

		var scanner = new ConfigScanner(fs);
		var result = scanner.Scan(CursorProfile, "/project", TestServers, expectedDefs);

		result.ServerResults["UnoApp"].Status.Should().Be("missing");
		result.ServerResults["UnoDocs"].Status.Should().Be("missing");
	}

	// ── Server outdated ──

	[TestMethod]
	public void Scan_StableButExpectedPrerelease_ReturnsOutdated()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		// Expected is prerelease
		var prereleaseDef = JsonNode.Parse("""{"command":"dnx","args":["-y","--prerelease","uno.devserver","--mcp-app"]}""")!.AsObject();
		var expectedDefs = new Dictionary<string, JsonObject> { ["UnoApp"] = prereleaseDef, ["UnoDocs"] = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject() };

		var scanner = new ConfigScanner(fs);
		var result = scanner.Scan(CursorProfile, "/project", TestServers, expectedDefs);

		result.ServerResults["UnoApp"].Status.Should().Be("outdated");
	}

	[TestMethod]
	public void Scan_LegacyHttp_ReturnsOutdated()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"url": "http://localhost:5000/mcp"}
		  }
		}
		""");

		var stableDef = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var expectedDefs = new Dictionary<string, JsonObject> { ["UnoApp"] = stableDef, ["UnoDocs"] = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject() };

		var scanner = new ConfigScanner(fs);
		var result = scanner.Scan(CursorProfile, "/project", TestServers, expectedDefs);

		result.ServerResults["UnoApp"].Status.Should().Be("outdated");
	}

	// ── Multiple scopes ──

	[TestMethod]
	public void Scan_MultipleScopes_WarnsAboutMultipleRegistrations()
	{
		var fs = new InMemoryFileSystem { HomePath = "/home/user" };
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");
		fs.AddFile("/home/user/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var stableDef = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var expectedDefs = new Dictionary<string, JsonObject> { ["UnoApp"] = stableDef, ["UnoDocs"] = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject() };

		var scanner = new ConfigScanner(fs);
		var result = scanner.Scan(CursorProfile, "/project", TestServers, expectedDefs);

		result.ServerResults["UnoApp"].Locations.Should().HaveCount(2);
		result.ServerResults["UnoApp"].Warnings.Should().Contain("Registered in multiple config files");
	}

	// ── IDE detection ──

	[TestMethod]
	public void Scan_DirectoryExists_DetectedTrue()
	{
		var fs = new InMemoryFileSystem();
		fs.AddDirectory("/project/.cursor");

		var expectedDefs = new Dictionary<string, JsonObject>();
		var scanner = new ConfigScanner(fs);
		var result = scanner.Scan(CursorProfile, "/project", TestServers, expectedDefs);

		result.Detected.Should().BeTrue();
	}

	[TestMethod]
	public void Scan_NoDirectory_DetectedFalse()
	{
		var fs = new InMemoryFileSystem();
		var expectedDefs = new Dictionary<string, JsonObject>();

		var scanner = new ConfigScanner(fs);
		var result = scanner.Scan(CursorProfile, "/project", TestServers, expectedDefs);

		result.Detected.Should().BeFalse();
	}

	// ── Path token resolution ──

	[TestMethod]
	public void ResolvePath_ReplacesAllTokens()
	{
		var result = ConfigScanner.ResolvePath(
			"{workspace}/.vscode/mcp.json", "/my/project", "/home/user", "/home/user/.config");
		result.Should().Be("/my/project/.vscode/mcp.json");
	}

	[TestMethod]
	public void ResolvePath_ReplacesHomeToken()
	{
		var result = ConfigScanner.ResolvePath(
			"{home}/.cursor/mcp.json", "/project", "/home/user", "/home/user/.config");
		result.Should().Be("/home/user/.cursor/mcp.json");
	}

	[TestMethod]
	public void ResolvePath_ReplacesAppDataToken()
	{
		var result = ConfigScanner.ResolvePath(
			"{appdata}/Code/User/settings.json", "/project", "/home/user", "/home/user/.config");
		result.Should().Be("/home/user/.config/Code/User/settings.json");
	}

	// ── Content-based detection with renamed key ──

	[TestMethod]
	public void Scan_RenamedKey_DetectedByContent()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "my-uno": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var stableDef = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject();
		var expectedDefs = new Dictionary<string, JsonObject> { ["UnoApp"] = stableDef, ["UnoDocs"] = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject() };

		var scanner = new ConfigScanner(fs);
		var result = scanner.Scan(CursorProfile, "/project", TestServers, expectedDefs);

		result.ServerResults["UnoApp"].Status.Should().Be("registered");
		result.ServerResults["UnoApp"].Locations.Should().HaveCount(1);
	}
}

using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_McpSetupOrchestrator
{
	private static readonly NullLogger<McpSetupOrchestrator> _logger = NullLogger<McpSetupOrchestrator>.Instance;

	private static Definitions TestDefs => new(
		Ides: new Dictionary<string, IdeProfile>(StringComparer.OrdinalIgnoreCase)
		{
			["cursor"] = new(
				ConfigPaths: ["{workspace}/.cursor/mcp.json", "{home}/.cursor/mcp.json"],
				WriteTarget: "{workspace}/.cursor/mcp.json",
				JsonRootKey: "mcpServers"),
			["vscode"] = new(
				ConfigPaths: ["{workspace}/.vscode/mcp.json", "{home}/.vscode/mcp.json"],
				WriteTarget: "{workspace}/.vscode/mcp.json",
				JsonRootKey: "servers",
				IncludeType: true),
			["antigravity"] = new(
				ConfigPaths: ["{home}/.gemini/antigravity/mcp_config.json"],
				WriteTarget: "{home}/.gemini/antigravity/mcp_config.json",
				JsonRootKey: "mcpServers",
				IncludeType: true,
				UrlKey: "serverUrl"),
			["windsurf"] = new(
				ConfigPaths: ["{workspace}/.windsurf/mcp.json", "{home}/.codeium/windsurf/mcp_config.json"],
				WriteTarget: "{workspace}/.windsurf/mcp.json",
				JsonRootKey: "mcpServers",
				UrlKey: "serverUrl"),
			["opencode"] = new(
				ConfigPaths: ["{workspace}/opencode.json", "{workspace}/opencode.jsonc"],
				WriteTarget: "{workspace}/opencode.json",
				JsonRootKey: "mcp",
				IncludeType: true,
				TypeMap: new Dictionary<string, string> { ["stdio"] = "local", ["http"] = "remote" },
				MergeCommandArgs: true),
		},
		Servers: new Dictionary<string, ServerDefinition>(StringComparer.OrdinalIgnoreCase)
		{
			["UnoApp"] = new(
				Transport: "stdio",
				Variants: new Dictionary<string, JsonObject>
				{
					["stable"] = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject(),
					["prerelease"] = JsonNode.Parse("""{"command":"dnx","args":["-y","--prerelease","uno.devserver","--mcp-app"]}""")!.AsObject(),
					["pinned"] = JsonNode.Parse("""{"command":"dnx","args":["-y","--version","{version}","uno.devserver","--mcp-app"]}""")!.AsObject(),
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
					["prerelease"] = JsonNode.Parse("""{"url":"https://mcp.platform.uno/v1"}""")!.AsObject(),
				},
				Detection: new(
					KeyPatterns: ["^UnoDocs$"],
					CommandPatterns: null,
					UrlPatterns: [@"mcp\.platform\.uno"])),
		});

	private static McpSetupOrchestrator CreateOrchestrator(IFileSystem fs)
	{
		var logger = _logger;
		return new McpSetupOrchestrator(fs, logger);
	}

	// ── Status ──

	[TestMethod]
	public void Status_NoConfigFiles_AllMissing()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		var result = orch.Status(TestDefs, "/project", null, "stable", "5.6.0");

		result.Version.Should().Be("1.0");
		result.ToolVersion.Should().Be("5.6.0");
		result.ExpectedVariant.Should().Be("stable");

		foreach (var server in result.Servers)
		{
			foreach (var ide in server.Ides)
			{
				ide.Status.Should().Be("missing");
			}
		}
	}

	[TestMethod]
	public void Status_ServerRegistered_ReturnsRegistered()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		var result = orch.Status(TestDefs, "/project", "cursor", "stable", "5.6.0");

		result.CallerIde.Should().Be("cursor");

		var unoApp = result.Servers.First(s => s.Name == "UnoApp");
		var cursorStatus = unoApp.Ides.First(i => i.Ide == "cursor");
		cursorStatus.Status.Should().Be("registered");
	}

	[TestMethod]
	public void Status_DetectsIdes()
	{
		var fs = new InMemoryFileSystem();
		fs.AddDirectory("/project/.cursor");

		var orch = CreateOrchestrator(fs);
		var result = orch.Status(TestDefs, "/project", null, "stable", "5.6.0");

		result.DetectedIdes.Should().Contain("cursor");
		result.DetectedIdes.Should().NotContain("vscode");
	}

	[TestMethod]
	public void Status_Serialization_IncludesDetectedIdesAndLocationTransport()
	{
		var fs = new InMemoryFileSystem();
		fs.AddDirectory("/project/.cursor");
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		var result = orch.Status(TestDefs, "/project", "cursor", "stable", "5.6.0");
		var json = JsonSerializer.Serialize(result, McpSetupJson.OutputOptions);

		json.Should().Contain("\"detectedIdes\"");
		json.Should().Contain("\"ide\": \"cursor\"");
		json.Should().Contain("\"transport\": \"stdio\"");
	}

	// ── Install ──

	[TestMethod]
	public void Install_MissingServer_CreatesFile()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		var result = orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", null);

		result.Version.Should().Be("1.0");
		var ops = result.Operations.Where(o => o.Server != "*").ToList();
		ops.Should().HaveCount(2);

		var unoAppOp = ops.First(o => o.Server == "UnoApp");
		unoAppOp.Action.Should().Be("created");

		// Verify file was created
		var content = fs.GetFileContent("/project/.cursor/mcp.json");
		content.Should().NotBeNull();
		var parsed = JsonNode.Parse(content!)!.AsObject();
		parsed["mcpServers"]!["UnoApp"]!["command"]!.GetValue<string>().Should().Be("dnx");
		parsed["mcpServers"]!["UnoDocs"]!["url"]!.GetValue<string>().Should().Be("https://mcp.platform.uno/v1");
	}

	[TestMethod]
	public void Install_OutdatedServer_UpdatesEntry()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		var result = orch.Install(TestDefs, "/project", "cursor", "prerelease", "5.6.0-dev.42", null);

		var unoAppOp = result.Operations.First(o => o.Server == "UnoApp");
		unoAppOp.Action.Should().Be("updated");

		var content = fs.GetFileContent("/project/.cursor/mcp.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var args = parsed["mcpServers"]!["UnoApp"]!["args"]!.AsArray();
		args.Select(a => a!.GetValue<string>()).Should().Contain("--prerelease");
	}

	[TestMethod]
	public void Install_AlreadyRegistered_SkipsEntry()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]},
		    "UnoDocs": {"url": "https://mcp.platform.uno/v1"}
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		var result = orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", null);

		result.Operations.Should().AllSatisfy(o => o.Action.Should().Be("skipped"));
	}

	[TestMethod]
	public void Install_ReadOnlyFile_ReportsError()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """{"mcpServers":{}}""");
		fs.SetReadOnly("/project/.cursor/mcp.json");

		var orch = CreateOrchestrator(fs);
		var result = orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		var op = result.Operations.First(o => o.Server == "UnoApp");
		op.Action.Should().Be("error");
		op.Reason.Should().Contain("read-only");
	}

	[TestMethod]
	public void Install_VsCodeFormat_IncludesType()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "vscode", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		var content = fs.GetFileContent("/project/.vscode/mcp.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["servers"]!["UnoApp"]!.AsObject();
		entry["type"]!.GetValue<string>().Should().Be("stdio");
	}

	[TestMethod]
	public void Install_CursorFormat_NoType()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		var content = fs.GetFileContent("/project/.cursor/mcp.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["mcpServers"]!["UnoApp"]!.AsObject();
		entry.ContainsKey("type").Should().BeFalse();
	}

	[TestMethod]
	public void Install_AntigravityFormat_WritesServerUrl()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "antigravity", "stable", "5.6.0", serverFilter: ["UnoDocs"]);

		var content = fs.GetFileContent("/home/testuser/.gemini/antigravity/mcp_config.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["mcpServers"]!["UnoDocs"]!.AsObject();
		entry["serverUrl"]!.GetValue<string>().Should().Be("https://mcp.platform.uno/v1");
		entry.ContainsKey("url").Should().BeFalse();
		entry["type"]!.GetValue<string>().Should().Be("http");
	}

	[TestMethod]
	public void Install_AntigravityFormat_StdioIncludesType()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "antigravity", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		var content = fs.GetFileContent("/home/testuser/.gemini/antigravity/mcp_config.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["mcpServers"]!["UnoApp"]!.AsObject();
		entry["type"]!.GetValue<string>().Should().Be("stdio");
	}

	[TestMethod]
	public void Install_WindsurfFormat_WritesServerUrl()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "windsurf", "stable", "5.6.0", serverFilter: ["UnoDocs"]);

		var content = fs.GetFileContent("/project/.windsurf/mcp.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["mcpServers"]!["UnoDocs"]!.AsObject();
		entry["serverUrl"]!.GetValue<string>().Should().Be("https://mcp.platform.uno/v1");
		entry.ContainsKey("url").Should().BeFalse();
		entry.ContainsKey("type").Should().BeFalse();
	}

	[TestMethod]
	public void Install_WindsurfFormat_StdioNoType()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "windsurf", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		var content = fs.GetFileContent("/project/.windsurf/mcp.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["mcpServers"]!["UnoApp"]!.AsObject();
		entry["command"]!.GetValue<string>().Should().Be("dnx");
		entry.ContainsKey("type").Should().BeFalse();
	}

	[TestMethod]
	public void Install_PinnedVersion_ReplacesPlaceholder()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "cursor", "pinned:5.5.0", "5.6.0", null);

		var content = fs.GetFileContent("/project/.cursor/mcp.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var args = parsed["mcpServers"]!["UnoApp"]!["args"]!.AsArray();
		args.Select(a => a!.GetValue<string>()).Should().Contain("5.5.0");
		args.Select(a => a!.GetValue<string>()).Should().NotContain("{version}");
	}

	[TestMethod]
	public void Install_ServerFilter_OnlyProcessesFiltered()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		var result = orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		result.Operations.Should().HaveCount(1);
		result.Operations[0].Server.Should().Be("UnoApp");
	}

	[TestMethod]
	public void Install_UnknownIde_ReportsError()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		var result = orch.Install(TestDefs, "/project", "unknown-ide", "stable", "5.6.0", null);

		result.Operations.Should().HaveCount(1);
		result.Operations[0].Action.Should().Be("error");
	}

	[TestMethod]
	public void Install_Serialization_IncludesOperationIde()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		var result = orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"]);
		var json = JsonSerializer.Serialize(result, McpSetupJson.OutputOptions);

		json.Should().Contain("\"ide\": \"cursor\"");
	}

	[TestMethod]
	public void Install_PreservesExistingEntries()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "MyOtherServer": {"command": "other-tool", "args": ["--run"]}
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", null);

		var content = fs.GetFileContent("/project/.cursor/mcp.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var servers = parsed["mcpServers"]!.AsObject();
		servers["MyOtherServer"].Should().NotBeNull();
		servers["UnoApp"].Should().NotBeNull();
		servers["UnoDocs"].Should().NotBeNull();
	}

	[TestMethod]
	public void Install_ShallowMerge_PreservesUserKeys()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {
		      "command": "dnx",
		      "args": ["-y", "uno.devserver", "--mcp-app"],
		      "env": {"MY_VAR": "test"},
		      "disabled": false
		    }
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		orch.Install(TestDefs, "/project", "cursor", "prerelease", "5.6.0-dev.42", serverFilter: ["UnoApp"]);

		var content = fs.GetFileContent("/project/.cursor/mcp.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["mcpServers"]!["UnoApp"]!.AsObject();
		entry["env"]!["MY_VAR"]!.GetValue<string>().Should().Be("test");
		entry["disabled"]!.GetValue<bool>().Should().BeFalse();
		// Args should be updated to prerelease
		entry["args"]!.AsArray().Select(a => a!.GetValue<string>()).Should().Contain("--prerelease");
	}

	// ── OpenCode format ──

	[TestMethod]
	public void Install_OpenCodeFormat_RemoteWritesTypeAndUrl()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "opencode", "stable", "5.6.0", serverFilter: ["UnoDocs"]);

		var content = fs.GetFileContent("/project/opencode.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["mcp"]!["UnoDocs"]!.AsObject();
		entry["type"]!.GetValue<string>().Should().Be("remote");
		entry["url"]!.GetValue<string>().Should().Be("https://mcp.platform.uno/v1");
		entry.ContainsKey("command").Should().BeFalse();
	}

	[TestMethod]
	public void Install_OpenCodeFormat_LocalWritesCommandArray()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "opencode", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		var content = fs.GetFileContent("/project/opencode.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["mcp"]!["UnoApp"]!.AsObject();
		entry["type"]!.GetValue<string>().Should().Be("local");
		entry["command"].Should().BeOfType<JsonArray>();
		var cmdArray = entry["command"]!.AsArray();
		cmdArray[0]!.GetValue<string>().Should().Be("dnx");
		cmdArray.Select(a => a!.GetValue<string>()).Should().Contain("uno.devserver");
		entry.ContainsKey("args").Should().BeFalse();
	}

	[TestMethod]
	public void Install_OpenCodeFormat_PreservesUserKeys()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/opencode.json", """
		{
		  "mcp": {
		    "UnoApp": {
		      "type": "local",
		      "command": ["dnx", "-y", "uno.devserver", "--mcp-app"],
		      "environment": {"MY_VAR": "test"},
		      "enabled": true
		    }
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		orch.Install(TestDefs, "/project", "opencode", "prerelease", "5.6.0-dev.42", serverFilter: ["UnoApp"]);

		var content = fs.GetFileContent("/project/opencode.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		var entry = parsed["mcp"]!["UnoApp"]!.AsObject();
		entry["environment"]!["MY_VAR"]!.GetValue<string>().Should().Be("test");
		entry["enabled"]!.GetValue<bool>().Should().BeTrue();
		// Command should be updated to prerelease
		var cmdArray = entry["command"]!.AsArray();
		cmdArray.Select(a => a!.GetValue<string>()).Should().Contain("--prerelease");
	}

	// ── Uninstall ──

	[TestMethod]
	public void Uninstall_RegisteredServer_RemovesEntry()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]},
		    "OtherServer": {"command": "other"}
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		var result = orch.Uninstall(TestDefs, "/project", "cursor", serverFilter: ["UnoApp"]);

		var op = result.Operations.First(o => o.Server == "UnoApp");
		op.Action.Should().Be("removed");

		var content = fs.GetFileContent("/project/.cursor/mcp.json")!;
		var parsed = JsonNode.Parse(content)!.AsObject();
		parsed["mcpServers"]!.AsObject().ContainsKey("UnoApp").Should().BeFalse();
		parsed["mcpServers"]!["OtherServer"].Should().NotBeNull();
	}

	[TestMethod]
	public void Uninstall_NotFound_ReportsNotFound()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		var result = orch.Uninstall(TestDefs, "/project", "cursor", serverFilter: ["UnoApp"]);

		var op = result.Operations.First(o => o.Server == "UnoApp");
		op.Action.Should().Be("not_found");
	}

	[TestMethod]
	public void Uninstall_DefaultScope_RemovesOnlyWriteTarget()
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

		var orch = CreateOrchestrator(fs);
		var result = orch.Uninstall(TestDefs, "/project", "cursor", serverFilter: ["UnoApp"]);

		var removedOps = result.Operations.Where(o => o.Server == "UnoApp" && o.Action == "removed").ToList();
		removedOps.Should().ContainSingle();
		removedOps[0].Path!.Replace('\\', '/').Should().Be("/project/.cursor/mcp.json");
		fs.GetFileContent("/home/user/.cursor/mcp.json").Should().NotBeNull();
	}

	[TestMethod]
	public void Uninstall_MultipleScopes_RemovesFromAll()
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

		var orch = CreateOrchestrator(fs);
		var result = orch.Uninstall(TestDefs, "/project", "cursor", serverFilter: ["UnoApp"], allScopes: true);

		var removedOps = result.Operations.Where(o => o.Server == "UnoApp" && o.Action == "removed").ToList();
		removedOps.Should().HaveCount(2);
	}

	[TestMethod]
	public void Uninstall_UnknownIde_ReportsError()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		var result = orch.Uninstall(TestDefs, "/project", "unknown-ide", null);

		result.Operations.Should().HaveCount(1);
		result.Operations[0].Action.Should().Be("error");
	}

	// ── R1: Note field ──

	[TestMethod]
	public void Install_ExistingFile_HasNote()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		var result = orch.Install(TestDefs, "/project", "cursor", "prerelease", "5.6.0-dev.42", null);

		var unoAppOp = result.Operations.First(o => o.Server == "UnoApp");
		unoAppOp.Note.Should().Contain("Modified cursor configuration file");
	}

	[TestMethod]
	public void Install_NewFile_NoNote()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		var result = orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		var op = result.Operations.First(o => o.Server == "UnoApp");
		op.Note.Should().BeNull();
	}

	[TestMethod]
	public void Install_Skipped_NoNote()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		var result = orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		var op = result.Operations.First(o => o.Server == "UnoApp");
		op.Action.Should().Be("skipped");
		op.Note.Should().BeNull();
	}

	// ── R4: Backup ──

	[TestMethod]
	public void Install_ExistingFile_CreatesBackup()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """{"mcpServers":{}}""");

		var orch = CreateOrchestrator(fs);
		orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		fs.Backups.Should().Contain("/project/.cursor/mcp.json");
		fs.GetFileContent("/project/.cursor/mcp.json.bak").Should().Be("""{"mcpServers":{}}""");
	}

	[TestMethod]
	public void Install_NewFile_NoBackup()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"]);

		fs.Backups.Should().BeEmpty();
	}

	[TestMethod]
	public void Uninstall_ExistingServer_CreatesBackup()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var orch = CreateOrchestrator(fs);
		orch.Uninstall(TestDefs, "/project", "cursor", serverFilter: ["UnoApp"]);

		fs.Backups.Should().NotBeEmpty();
	}

	[TestMethod]
	public void Install_MultipleServers_BackupPreservesOriginal()
	{
		var fs = new InMemoryFileSystem();
		var originalContent = """{"mcpServers":{}}""";
		fs.AddFile("/project/.cursor/mcp.json", originalContent);

		var orch = CreateOrchestrator(fs);
		// Install both UnoApp and UnoDocs — both write to same file
		orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", null);

		// The .bak should contain the original content, not an intermediate state
		fs.GetFileContent("/project/.cursor/mcp.json.bak").Should().Be(originalContent);
		// But the actual file should have both servers
		var content = fs.GetFileContent("/project/.cursor/mcp.json")!;
		var parsed = System.Text.Json.Nodes.JsonNode.Parse(content)!.AsObject();
		parsed["mcpServers"]!.AsObject().ContainsKey("UnoApp").Should().BeTrue();
		parsed["mcpServers"]!.AsObject().ContainsKey("UnoDocs").Should().BeTrue();
	}

	// ── R2: Dry-run ──

	[TestMethod]
	public void Install_DryRun_DoesNotWriteFile()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"], dryRun: true);

		fs.GetFileContent("/project/.cursor/mcp.json").Should().BeNull();
	}

	[TestMethod]
	public void Install_DryRun_ReturnsCorrectOperations()
	{
		var fs = new InMemoryFileSystem();
		var orch = CreateOrchestrator(fs);

		var result = orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", null, dryRun: true);

		result.Operations.Should().HaveCount(2);
		result.Operations.Should().AllSatisfy(o => o.Action.Should().Be("created"));
	}

	[TestMethod]
	public void Install_DryRun_NoBackup()
	{
		var fs = new InMemoryFileSystem();
		fs.AddFile("/project/.cursor/mcp.json", """{"mcpServers":{}}""");

		var orch = CreateOrchestrator(fs);
		orch.Install(TestDefs, "/project", "cursor", "stable", "5.6.0", serverFilter: ["UnoApp"], dryRun: true);

		fs.Backups.Should().BeEmpty();
	}

	[TestMethod]
	public void Uninstall_DryRun_DoesNotModifyFile()
	{
		var fs = new InMemoryFileSystem();
		var originalContent = """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""";
		fs.AddFile("/project/.cursor/mcp.json", originalContent);

		var orch = CreateOrchestrator(fs);
		var result = orch.Uninstall(TestDefs, "/project", "cursor", serverFilter: ["UnoApp"], dryRun: true);

		result.Operations.First(o => o.Server == "UnoApp").Action.Should().Be("removed");
		fs.GetFileContent("/project/.cursor/mcp.json").Should().Be(originalContent);
		fs.Backups.Should().BeEmpty();
	}
}

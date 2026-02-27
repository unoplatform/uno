using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_McpSetupOrchestrator
{
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
				JsonRootKey: "servers"),
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
		using var loggerFactory = LoggerFactory.Create(b => { });
		return new McpSetupOrchestrator(fs, loggerFactory.CreateLogger<McpSetupOrchestrator>());
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
		var result = orch.Uninstall(TestDefs, "/project", "cursor", serverFilter: ["UnoApp"]);

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
}

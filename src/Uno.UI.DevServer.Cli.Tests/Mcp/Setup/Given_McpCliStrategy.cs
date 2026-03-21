using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_McpCliStrategy
{
	private static readonly NullLogger<McpSetupOrchestrator> _logger = NullLogger<McpSetupOrchestrator>.Instance;
	private const string Workspace = "/tmp/test-workspace";

	private static IdeProfile ProfileWithCli(bool cliAvailable = true) => new(
		ConfigPaths: ["{workspace}/.mcp.json"],
		WriteTarget: "{workspace}/.mcp.json",
		JsonRootKey: "mcpServers",
		Cli: new CliProfile(
			Executable: "test-agent",
			Detect: ["--version"],
			AddStdio: ["mcp", "add", "--transport", "stdio", "{name}", "--", "{command}", "{args...}"],
			AddHttp: ["mcp", "add", "--transport", "http", "{name}", "{url}"],
			List: ["mcp", "list"],
			Remove: ["mcp", "remove", "{name}"]));

	private static IdeProfile ProfileWithCliNoRemove() => new(
		ConfigPaths: ["{workspace}/.mcp.json"],
		WriteTarget: "{workspace}/.mcp.json",
		JsonRootKey: "mcpServers",
		Cli: new CliProfile(
			Executable: "test-agent",
			Detect: ["--version"],
			AddStdio: ["mcp", "add", "{name}"],
			AddHttp: null,
			List: ["mcp", "list"],
			Remove: null));

	private static Definitions DefsWithProfile(IdeProfile profile) => new(
		Ides: new Dictionary<string, IdeProfile>(StringComparer.OrdinalIgnoreCase)
		{
			["test-agent"] = profile,
		},
		Servers: new Dictionary<string, ServerDefinition>(StringComparer.OrdinalIgnoreCase)
		{
			["UnoApp"] = new("stdio",
				new Dictionary<string, JsonObject>
				{
					["stable"] = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject()
				},
				new DetectionPatterns(["^UnoApp$"], ["uno\\.devserver"], null)),
		});

	// ──────────────────────────────────────────────
	// Install
	// ──────────────────────────────────────────────

	[TestMethod]
	public void Install_CliAvailable_DelegatesAndReturnsCreated()
	{
		var mock = new MockCliRunner(available: true, exitCode: 0);
		var fs = new InMemoryFileSystem();
		var orchestrator = new McpSetupOrchestrator(fs, _logger, mock);

		var result = orchestrator.Install(
			DefsWithProfile(ProfileWithCli()), Workspace, "test-agent",
			"stable", "1.0.0", serverFilter: null);

		result.Operations.Should().HaveCount(1);
		result.Operations[0].Action.Should().Be("created");
		result.Operations[0].Note.Should().Contain("CLI");
		mock.ExecuteCalls.Should().BeGreaterThan(0);
	}

	[TestMethod]
	public void Install_CliFailsNonZero_FallsBackToFile()
	{
		var mock = new MockCliRunner(available: true, exitCode: 1, stderr: "some error");
		var fs = new InMemoryFileSystem();
		// Ensure the write target directory exists for fallback
		fs.CreateDirectory(Workspace + "/.mcp.json".Replace("/.mcp.json", ""));
		var orchestrator = new McpSetupOrchestrator(fs, _logger, mock);

		var result = orchestrator.Install(
			DefsWithProfile(ProfileWithCli()), Workspace, "test-agent",
			"stable", "1.0.0", serverFilter: null);

		// Should fall back to file and create the entry
		result.Operations.Should().HaveCount(1);
		result.Operations[0].Action.Should().Be("created");
		result.Operations[0].Note.Should().NotContain("CLI");
	}

	[TestMethod]
	public void Install_CliNotInPath_FallsBackToFile()
	{
		var mock = new MockCliRunner(available: false);
		var fs = new InMemoryFileSystem();
		var orchestrator = new McpSetupOrchestrator(fs, _logger, mock);

		var result = orchestrator.Install(
			DefsWithProfile(ProfileWithCli()), Workspace, "test-agent",
			"stable", "1.0.0", serverFilter: null);

		// Should fall back to file
		result.Operations.Should().HaveCount(1);
		result.Operations[0].Action.Should().Be("created");
		mock.ExecuteCalls.Should().Be(0);
	}

	[TestMethod]
	public void Install_NullRunner_FallsBackToFile()
	{
		var fs = new InMemoryFileSystem();
		var orchestrator = new McpSetupOrchestrator(fs, _logger, cliRunner: null);

		var result = orchestrator.Install(
			DefsWithProfile(ProfileWithCli()), Workspace, "test-agent",
			"stable", "1.0.0", serverFilter: null);

		result.Operations.Should().HaveCount(1);
		result.Operations[0].Action.Should().Be("created");
	}

	[TestMethod]
	public void Install_DryRun_ShowsCommandWithoutExecuting()
	{
		var mock = new MockCliRunner(available: true);
		var fs = new InMemoryFileSystem();
		var orchestrator = new McpSetupOrchestrator(fs, _logger, mock);

		var result = orchestrator.Install(
			DefsWithProfile(ProfileWithCli()), Workspace, "test-agent",
			"stable", "1.0.0", serverFilter: null, dryRun: true);

		result.Operations.Should().HaveCount(1);
		result.Operations[0].Action.Should().Be("created");
		result.Operations[0].Note.Should().Contain("Dry-run");
		result.Operations[0].Note.Should().Contain("test-agent");
		mock.ExecuteCalls.Should().Be(0);
	}

	// ──────────────────────────────────────────────
	// Uninstall
	// ──────────────────────────────────────────────

	[TestMethod]
	public void Uninstall_CliAvailable_DelegatesAndReturnsRemoved()
	{
		var mock = new MockCliRunner(available: true, exitCode: 0);
		var fs = new InMemoryFileSystem();
		var orchestrator = new McpSetupOrchestrator(fs, _logger, mock);

		var result = orchestrator.Uninstall(
			DefsWithProfile(ProfileWithCli()), Workspace, "test-agent",
			serverFilter: null);

		result.Operations.Should().HaveCount(1);
		result.Operations[0].Action.Should().Be("removed");
		result.Operations[0].Note.Should().Contain("CLI");
	}

	[TestMethod]
	public void Uninstall_CliRemoveNull_FallsBackToFile()
	{
		var mock = new MockCliRunner(available: true, exitCode: 0);
		var fs = new InMemoryFileSystem();
		var orchestrator = new McpSetupOrchestrator(fs, _logger, mock);

		var result = orchestrator.Uninstall(
			DefsWithProfile(ProfileWithCliNoRemove()), Workspace, "test-agent",
			serverFilter: null);

		// Remove is null → falls back to file path → not_found (no file exists)
		result.Operations.Should().HaveCount(1);
		result.Operations[0].Action.Should().Be("not_found");
	}

	// ──────────────────────────────────────────────
	// Mock
	// ──────────────────────────────────────────────

	private sealed class MockCliRunner : CliCommandRunner
	{
		private readonly bool _available;
		private readonly int _exitCode;
		private readonly string _stderr;

		public int ExecuteCalls { get; private set; }

		public MockCliRunner(bool available, int exitCode = 0, string stderr = "")
			: base(NullLogger<CliCommandRunner>.Instance)
		{
			_available = available;
			_exitCode = exitCode;
			_stderr = stderr;
		}

		public override bool IsAvailable(CliProfile cli) => _available;

		public override (int ExitCode, string Stdout, string Stderr) Execute(
			string executable, string[] argsTemplate,
			IDictionary<string, object> placeholders,
			string workingDirectory, TimeSpan? timeout = null)
		{
			ExecuteCalls++;
			if (_exitCode != 0)
			{
				return (_exitCode, "", _stderr);
			}
			return (0, "ok", "");
		}
	}
}

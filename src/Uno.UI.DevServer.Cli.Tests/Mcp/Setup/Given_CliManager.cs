using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_CliManager
{
	[TestMethod]
	public void DetermineMcpSetupExitCode_PartialError_ReturnsZero()
	{
		var result = new OperationResponse("1.0", [
			new OperationEntry("UnoApp", "cursor", "created", "/project/.cursor/mcp.json", null),
			new OperationEntry("UnoDocs", "cursor", "error", "/project/.cursor/mcp.json", "File is read-only"),
		]);

		CliManager.DetermineMcpSetupExitCode(result).Should().Be(0);
	}

	[TestMethod]
	public void DetermineMcpSetupExitCode_AllSuccess_ReturnsZero()
	{
		var result = new OperationResponse("1.0", [
			new OperationEntry("UnoApp", "cursor", "created", "/project/.cursor/mcp.json", null),
		]);

		CliManager.DetermineMcpSetupExitCode(result).Should().Be(0);
	}

	[TestMethod]
	public void DetermineMcpSetupExitCode_AllFailures_ReturnsOne()
	{
		var result = new OperationResponse("1.0", [
			new OperationEntry("UnoApp", "cursor", "error", "/project/.cursor/mcp.json", "File is read-only"),
			new OperationEntry("UnoDocs", "cursor", "not_found", null, null),
		]);

		CliManager.DetermineMcpSetupExitCode(result).Should().Be(1);
	}

	[TestMethod]
	public void ParseMcpSetupArgs_MissingToolVersionValue_ReturnsNull()
	{
		var manager = CreateManager();

		var result = manager.ParseMcpSetupArgs(["--tool-version"], "status");

		result.Should().BeNull();
	}

	[TestMethod]
	public void ParseMcpSetupArgs_Channel_SetsValue()
	{
		var manager = CreateManager();

		var result = manager.ParseMcpSetupArgs(["--channel", "prerelease"], "status");

		result.Should().NotBeNull();
		result!.Value.Channel.Should().Be("prerelease");
	}

	[TestMethod]
	public void ParseMcpSetupArgs_ToolVersion_SetsValue()
	{
		var manager = CreateManager();

		var result = manager.ParseMcpSetupArgs(["--tool-version", "5.5.0"], "install");

		result.Should().NotBeNull();
		result!.Value.ToolVersion.Should().Be("5.5.0");
	}

	[TestMethod]
	public void ParseMcpSetupArgs_AllScopes_SetsFlag()
	{
		var manager = CreateManager();

		var result = manager.ParseMcpSetupArgs(["cursor", "--all-scopes"], "uninstall");

		result.Should().NotBeNull();
		result!.Value.AllScopes.Should().BeTrue();
	}

	[TestMethod]
	public void ParseMcpSetupArgs_EmptyServersValue_ReturnsNull()
	{
		var manager = CreateManager();

		var result = manager.ParseMcpSetupArgs(["cursor", "--servers", ","], "install");

		result.Should().BeNull();
	}

	[TestMethod]
	public void RunMcpSubcommand_ConflictingVariantOptions_ReturnsUsageError()
	{
		var manager = CreateManager();
		var workspace = CreateWorkspacePath();

		var result = manager.RunMcpSubcommand(["status", "--channel", "stable", "--tool-version", "5.5.0"], workspace);

		result.Should().Be(2);
	}

	[TestMethod]
	public void RunMcpSubcommand_InvalidChannel_ReturnsUsageError()
	{
		var manager = CreateManager();
		var workspace = CreateWorkspacePath();

		var result = manager.RunMcpSubcommand(["status", "--channel", "nightly"], workspace);

		result.Should().Be(2);
	}

	[TestMethod]
	public void RunMcpSubcommand_StatusUnknown_ReturnsSuccess()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		var manager = CreateManager(fs);
		var previousOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			var result = manager.RunMcpSubcommand(["status", "unknown", "--json"], workspace);

			result.Should().Be(0);
			writer.ToString().Should().Contain("\"callerIde\": \"unknown\"");
		}
		finally
		{
			Console.SetOut(previousOut);
		}
	}

	[TestMethod]
	public void RunMcpSubcommand_InstallNoIde_InstallsInAllDetected()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		fs.AddDirectory(Path.Combine(workspace, ".cursor"));

		var manager = CreateManager(fs);
		var previousOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			var result = manager.RunMcpSubcommand(["install", "--all-ides", "--json"], workspace);

			result.Should().Be(0);
			var output = writer.ToString();
			output.Should().Contain("\"ide\": \"cursor\"");
			output.Should().Contain("\"action\": \"created\"");
		}
		finally
		{
			Console.SetOut(previousOut);
		}
	}

	[TestMethod]
	public void RunMcpSubcommand_InstallNoIde_InstallsWorkspaceBackedProfiles()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		var manager = CreateManager(fs);
		var previousOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			var result = manager.RunMcpSubcommand(["install", "--all-ides", "--json"], workspace);

			result.Should().Be(0);
			var output = writer.ToString();
			output.Should().Contain("\"ide\": \"claude-code\"");
			output.Should().Contain("\"ide\": \"opencode\"");
		}
		finally
		{
			Console.SetOut(previousOut);
		}
	}

	[TestMethod]
	public void RunMcpSubcommand_InstallNoIde_WithVsCodeWorkspace_InstallsCopilotVsCode()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		fs.AddDirectory(Path.Combine(workspace, ".vscode"));
		var manager = CreateManager(fs);
		var previousOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			var result = manager.RunMcpSubcommand(["install", "--all-ides", "--json"], workspace);

			result.Should().Be(0);
			writer.ToString().Should().Contain("\"ide\": \"copilot-vscode\"");
		}
		finally
		{
			Console.SetOut(previousOut);
		}
	}

	[TestMethod]
	public void RunMcpSubcommand_InstallWithChannelPrerelease_WritesPrereleaseDefinition()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		var manager = CreateManager(fs);

		var result = manager.RunMcpSubcommand(["install", "cursor", "--channel", "prerelease"], workspace);

		result.Should().Be(0);
		var content = fs.GetFileContent(Path.Combine(workspace, ".cursor", "mcp.json"));
		content.Should().NotBeNull();
		content.Should().Contain("--prerelease");
	}

	[TestMethod]
	public void RunMcpSubcommand_InstallWithToolVersion_WritesPinnedDefinition()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		var manager = CreateManager(fs);

		var result = manager.RunMcpSubcommand(["install", "cursor", "--tool-version", "5.5.0"], workspace);

		result.Should().Be(0);
		var content = fs.GetFileContent(Path.Combine(workspace, ".cursor", "mcp.json"));
		content.Should().NotBeNull();
		content.Should().Contain("\"5.5.0\"");
	}

	[TestMethod]
	public void RunMcpSubcommand_InstallCopilotVs_WritesVsConfigFile()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		var manager = CreateManager(fs);

		var result = manager.RunMcpSubcommand(["install", "copilot-vs", "--servers", "UnoApp"], workspace);

		result.Should().Be(0);
		var content = fs.GetFileContent(Path.Combine(workspace, ".vs", "mcp.json"));
		content.Should().NotBeNull();
		content.Should().Contain("\"servers\"");
	}

	[TestMethod]
	public void RunMcpSubcommand_InstallCopilotCli_CreatesFileBasedRegistration()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		var manager = CreateManager(fs);
		var previousOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			var result = manager.RunMcpSubcommand(["install", "copilot-cli", "--json"], workspace);

			result.Should().Be(0);
			var output = writer.ToString();
			output.Should().Contain("\"ide\": \"copilot-cli\"");
			output.Should().Contain("\"action\": \"created\"");
		}
		finally
		{
			Console.SetOut(previousOut);
		}
	}

	[TestMethod]
	public void RunMcpSubcommand_StatusJson_IncludesSupportedIdesMetadata()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		var manager = CreateManager(fs);
		var previousOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			var result = manager.RunMcpSubcommand(["status", "--json"], workspace);

			result.Should().Be(0);
			var output = writer.ToString();
			output.Should().Contain("\"supportedIdes\"");
			output.Should().Contain("\"ide\": \"copilot-cli\"");
			output.Should().Contain("\"strategy\": \"native\"");
		}
		finally
		{
			Console.SetOut(previousOut);
		}
	}

	[TestMethod]
	public void RunMcpSubcommand_UninstallNoIde_UninstallsFromAllDetected()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		fs.AddDirectory(Path.Combine(workspace, ".cursor"));
		fs.AddFile(Path.Combine(workspace, ".cursor", "mcp.json"), """
		{
		  "mcpServers": {
		    "UnoApp": {"command": "dnx", "args": ["-y", "uno.devserver", "--mcp-app"]}
		  }
		}
		""");

		var manager = CreateManager(fs);
		var previousOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			var result = manager.RunMcpSubcommand(["uninstall", "--all-ides", "--json"], workspace);

			result.Should().Be(0);
			var output = writer.ToString();
			output.Should().Contain("\"ide\": \"cursor\"");
			output.Should().Contain("\"action\": \"removed\"");
		}
		finally
		{
			Console.SetOut(previousOut);
		}
	}

	[TestMethod]
	public void RunMcpSubcommand_InstallNoIde_NoAllIdesFlag_ReturnsUsageError()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		fs.AddDirectory(Path.Combine(workspace, ".cursor"));
		var manager = CreateManager(fs);

		var result = manager.RunMcpSubcommand(["install", "--json"], workspace);

		result.Should().Be(2);
	}

	[TestMethod]
	public void RunMcpSubcommand_UninstallNoIde_NoAllIdesFlag_ReturnsUsageError()
	{
		var workspace = CreateWorkspacePath();
		var fs = new InMemoryFileSystem();
		fs.AddDirectory(workspace);
		fs.AddDirectory(Path.Combine(workspace, ".cursor"));
		var manager = CreateManager(fs);

		var result = manager.RunMcpSubcommand(["uninstall", "--json"], workspace);

		result.Should().Be(2);
	}

	[TestMethod]
	public void ParseMcpSetupArgs_DryRun_SetsFlag()
	{
		var manager = CreateManager();

		var result = manager.ParseMcpSetupArgs(["cursor", "--dry-run"], "install");

		result.Should().NotBeNull();
		result!.Value.DryRun.Should().BeTrue();
	}

	[TestMethod]
	public void RunMcpSubcommand_WorkspaceDoesNotExist_ReturnsUsageError()
	{
		var manager = CreateManager(new FileSystem());
		var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

		var result = manager.RunMcpSubcommand(["status", "cursor", "--workspace", missingPath, "--json"], "/project");

		result.Should().Be(2);
	}

	[TestMethod]
	public void RunMcpSubcommand_WorkspaceRoot_ReturnsUsageError()
	{
		var manager = CreateManager(new FileSystem());
		var rootPath = Path.GetPathRoot(Environment.CurrentDirectory)!;

		var result = manager.RunMcpSubcommand(["status", "cursor", "--workspace", rootPath, "--json"], "/project");

		result.Should().Be(2);
	}

	private static string CreateWorkspacePath() =>
		Path.GetFullPath(Path.Combine(Path.GetTempPath(), "uno-mcp-tests", Guid.NewGuid().ToString("N")));

	private static CliManager CreateManager() => CreateManager(new InMemoryFileSystem());

	private static CliManager CreateManager(IFileSystem fs)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton(fs);
		services.AddSingleton<McpSetupOrchestrator>();

		services.AddSingleton<IWorkspaceResolver, NullWorkspaceResolver>();

		var provider = services.BuildServiceProvider();
		return new CliManager(
			provider,
			new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance),
			provider.GetRequiredService<IWorkspaceResolver>());
	}

	private class NullWorkspaceResolver : IWorkspaceResolver
	{
		public Task<WorkspaceResolution> ResolveAsync(string requestedDirectory) =>
			Task.FromResult(new WorkspaceResolution
			{
				RequestedWorkingDirectory = requestedDirectory,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates
			});

		public Task<WorkspaceResolution> ResolveExplicitWorkspaceAsync(string requestedDirectory) =>
			ResolveAsync(requestedDirectory);

		public Task<WorkspaceResolution> ResolveSolutionAsync(
			string requestedDirectory, string solutionPath,
			WorkspaceSelectionSource selectionSource = WorkspaceSelectionSource.UserSelected) =>
			ResolveAsync(requestedDirectory);
	}
}

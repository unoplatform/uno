using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
[DoNotParallelize]
public class Given_CliManager
{
	[TestMethod]
	[Description("The version banner used by MCP mode is logged through the logger so it appears in debug output instead of stdout")]
	public void LogVersionBanner_WritesVersionToLogger()
	{
		var loggerProvider = new CollectingLoggerProvider();
		var services = new ServiceCollection()
			.AddLogging(builder =>
			{
				builder.ClearProviders();
				builder.AddProvider(loggerProvider);
				builder.SetMinimumLevel(LogLevel.Information);
			})
			.BuildServiceProvider();

		var cliManager = new CliManager(
			services,
			new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance),
			new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance));

		cliManager.LogVersionBanner();

		loggerProvider.Messages.Should().ContainSingle(message => message.StartsWith("Uno Platform DevServer CLI - Version ", StringComparison.Ordinal));
	}

	[TestMethod]
	[Description("health --json returns the same health report shape used by uno_health when no workspace can be resolved")]
	public async Task HealthJson_WhenWorkspaceIsMissing_ReturnsImmediateUnhealthyReport()
	{
		var previousDirectory = Environment.CurrentDirectory;
		var temporaryDirectory = CreateTempDirectory();

		try
		{
			Environment.CurrentDirectory = temporaryDirectory;

			var services = new ServiceCollection()
				.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None))
				.BuildServiceProvider();

			var cliManager = new CliManager(
				services,
				new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance),
				new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance));

			using var stdout = new StringWriter();
			var previousOut = Console.Out;
			Console.SetOut(stdout);

			try
			{
				var exitCode = await cliManager.RunAsync(["health", "--json"]);

				exitCode.Should().Be(1);
			}
			finally
			{
				Console.SetOut(previousOut);
			}

			using var document = JsonDocument.Parse(stdout.ToString());
			var root = document.RootElement;

			root.GetProperty("status").GetString().Should().Be("Unhealthy");
			root.GetProperty("issues").EnumerateArray()
				.Select(issue => issue.GetProperty("code").GetString())
				.Should()
				.Contain(["HostNotStarted", "NoSolutionFound"]);
		}
		finally
		{
			Environment.CurrentDirectory = previousDirectory;
			Directory.Delete(temporaryDirectory, recursive: true);
		}
	}

	[TestMethod]
	[Description("health in plain text reports immediate unhealthy status and unresolved workspace details when no Uno workspace can be resolved")]
	public async Task HealthPlainText_WhenWorkspaceIsMissing_PrintsImmediateDiagnostics()
	{
		var previousDirectory = Environment.CurrentDirectory;
		var temporaryDirectory = CreateTempDirectory();

		try
		{
			Environment.CurrentDirectory = temporaryDirectory;

			var services = new ServiceCollection()
				.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None))
				.BuildServiceProvider();

			var cliManager = new CliManager(
				services,
				new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance),
				new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance));

			using var stdout = new StringWriter();
			var previousOut = Console.Out;
			Console.SetOut(stdout);

			try
			{
				var exitCode = await cliManager.RunAsync(["health"]);

				exitCode.Should().Be(1);
			}
			finally
			{
				Console.SetOut(previousOut);
			}

			var output = stdout.ToString();
			output.Should().Contain("Status: Unhealthy");
			output.Should().Contain("Workspace: <unresolved>");
			output.Should().Contain("Resolution: NoCandidates");
			output.Should().Contain("NoSolutionFound");
		}
		finally
		{
			Environment.CurrentDirectory = previousDirectory;
			Directory.Delete(temporaryDirectory, recursive: true);
		}
	}

	[TestMethod]
	[Description("health --json reports the effective workspace and selected solution when a nested Uno workspace is auto-discovered")]
	public async Task HealthJson_WhenNestedUnoWorkspaceExists_ReportsSelectedWorkspaceDetails()
	{
		var previousDirectory = Environment.CurrentDirectory;
		var temporaryDirectory = CreateTempDirectory();

		try
		{
			Environment.CurrentDirectory = temporaryDirectory;
			var workspace = Path.Combine(temporaryDirectory, "src");
			Directory.CreateDirectory(workspace);
			await File.WriteAllTextAsync(Path.Combine(workspace, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(workspace, "MyApp.slnx"), string.Empty);

			var services = new ServiceCollection()
				.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None))
				.BuildServiceProvider();

			var cliManager = new CliManager(
				services,
				new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance),
				new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance));

			using var stdout = new StringWriter();
			var previousOut = Console.Out;
			Console.SetOut(stdout);

			try
			{
				var exitCode = await cliManager.RunAsync(["health", "--json"]);

				exitCode.Should().Be(1);
			}
			finally
			{
				Console.SetOut(previousOut);
			}

			using var document = JsonDocument.Parse(stdout.ToString());
			var root = document.RootElement;

			root.GetProperty("effectiveWorkspaceDirectory").GetString().Should().Be(workspace);
			root.GetProperty("selectedSolutionPath").GetString().Should().Be(Path.Combine(workspace, "MyApp.slnx"));
			root.GetProperty("resolutionKind").GetString().Should().Be("AutoDiscovered");
			root.GetProperty("discoveredSolutions").ValueKind.Should().Be(JsonValueKind.Null);
		}
		finally
		{
			Environment.CurrentDirectory = previousDirectory;
			Directory.Delete(temporaryDirectory, recursive: true);
		}
	}

	[TestMethod]
	[Description("Non-workspace commands like list do not force a workspace resolution scan before invoking the host")]
	public async Task List_WhenWorkspaceResolutionIsNotRequired_DoesNotResolveWorkspace()
	{
		var services = new ServiceCollection()
			.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None))
			.BuildServiceProvider();
		var resolver = new ThrowingWorkspaceResolver();
		var cliManager = new CliManager(
			services,
			new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance),
			resolver);

		var exitCode = await cliManager.RunAsync(["list"]);

		exitCode.Should().Be(1);
		resolver.ResolveAsyncCallCount.Should().Be(0);
	}

	[TestMethod]
	[Description("Without --solution the MCP bridge arguments are left untouched")]
	public void ParseMcpSolutionArgument_WhenAbsent_ReturnsArgumentsUnchanged()
	{
		var result = CliManager.ParseMcpSolutionArgument(["--port", "0", "--force-roots-fallback"], Path.GetTempPath());

		result.Success.Should().BeTrue();
		result.SolutionPath.Should().BeNull();
		result.FilteredArgs.Should().Equal("--port", "0", "--force-roots-fallback");
	}

	[TestMethod]
	[Description("--solution is consumed by the MCP bridge (resolved against the working directory) instead of being forwarded to the host, which receives the bridge's own --solution (#25524)")]
	public void ParseMcpSolutionArgument_WhenPresent_ResolvesThePathAndRemovesItFromForwardedArguments()
	{
		var baseDirectory = CreateTempDirectory();

		try
		{
			var solutionPath = CreateSolutionFile(baseDirectory, "a", "App.slnx");

			var result = CliManager.ParseMcpSolutionArgument(["--port", "0", "--solution", Path.Combine("a", "App.slnx")], baseDirectory);

			result.Success.Should().BeTrue();
			result.SolutionPath.Should().Be(solutionPath);
			result.FilteredArgs.Should().Equal("--port", "0");
		}
		finally
		{
			Directory.Delete(baseDirectory, recursive: true);
		}
	}

	[TestMethod]
	[Description("Every spelling the host's command-line configuration reads as 'solution' is consumed, so none reaches the host past the checks")]
	[DataRow("--solution={0}")]
	[DataRow("--Solution={0}")]
	[DataRow("/solution={0}")]
	public void ParseMcpSolutionArgument_WhenInlineOrAlternateSpelling_ConsumesIt(string format)
	{
		var baseDirectory = CreateTempDirectory();

		try
		{
			var solutionPath = CreateSolutionFile(baseDirectory, "App.slnx");

			var result = CliManager.ParseMcpSolutionArgument(["--port", "0", string.Format(System.Globalization.CultureInfo.InvariantCulture, format, solutionPath)], baseDirectory);

			result.Success.Should().BeTrue();
			result.SolutionPath.Should().Be(solutionPath);
			result.FilteredArgs.Should().Equal("--port", "0");
		}
		finally
		{
			Directory.Delete(baseDirectory, recursive: true);
		}
	}

	[TestMethod]
	[Description("The same file checks as uno_app_select_solution: a non-solution file and a missing file are refused")]
	[DataRow("App.csproj", true, ".sln")]
	[DataRow("Missing.slnx", false, "not found")]
	public void ParseMcpSolutionArgument_WhenNotAnExistingSolution_Fails(string fileName, bool createFile, string expectedError)
	{
		var baseDirectory = CreateTempDirectory();

		try
		{
			var path = Path.Combine(baseDirectory, fileName);
			if (createFile)
			{
				File.WriteAllText(path, string.Empty);
			}

			var result = CliManager.ParseMcpSolutionArgument(["--solution", path], baseDirectory);

			result.Success.Should().BeFalse();
			result.Error.Should().Contain(expectedError);
		}
		finally
		{
			Directory.Delete(baseDirectory, recursive: true);
		}
	}

	[TestMethod]
	[Description("Options that only look like --solution are forwarded untouched")]
	[DataRow("/solutions")]
	[DataRow("--solutionX")]
	[DataRow("--solution-dir=x")]
	[DataRow("solution")]
	public void ParseMcpSolutionArgument_WhenOptionOnlyLooksLikeSolution_ForwardsIt(string arg)
	{
		var result = CliManager.ParseMcpSolutionArgument([arg], Path.GetTempPath());

		result.Success.Should().BeTrue();
		result.SolutionPath.Should().BeNull();
		result.FilteredArgs.Should().Equal(arg);
	}

	[TestMethod]
	[Description("--solution given twice is a usage error rather than a silent last-wins")]
	public void ParseMcpSolutionArgument_WhenGivenTwice_Fails()
	{
		var result = CliManager.ParseMcpSolutionArgument(["--solution", "A.slnx", "--solution=B.slnx"], Path.GetTempPath());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("more than once");
	}

	[TestMethod]
	[Description("--mcp-app --solution selects that solution explicitly instead of discovering the workspace; --solution-dir stays the requested directory (#25524)")]
	public async Task McpApp_WithSolution_ResolvesTheSolutionExplicitly()
	{
		var directory = CreateTempDirectory();

		try
		{
			var solutionPath = CreateSolutionFile(directory, "deep", "App.slnx");
			var resolver = new ThrowingWorkspaceResolver();
			var cliManager = CreateCliManager(resolver);

			var exitCode = await cliManager.RunAsync(["--solution-dir", directory, "--mcp-app", "--solution", solutionPath]);

			exitCode.Should().Be(1, "the throwing resolver stops the run before the proxy starts");
			resolver.SolutionRequests.Should().Equal((directory, solutionPath));
			resolver.ResolveAsyncCallCount.Should().Be(0);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	[Description("mcp serve with an unusable --solution exits with the usage error code before resolving anything")]
	public async Task McpServe_WithMissingSolution_ExitsWithUsageError()
	{
		var resolver = new ThrowingWorkspaceResolver();
		var cliManager = CreateCliManager(resolver);

		var exitCode = await cliManager.RunAsync(["mcp", "serve", "--solution", Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.slnx")]);

		exitCode.Should().Be(2);
		resolver.SolutionRequests.Should().BeEmpty();
		resolver.ResolveAsyncCallCount.Should().Be(0);
	}

	[TestMethod]
	[Description("--solution without a value is a usage error")]
	public void ParseMcpSolutionArgument_WhenValueIsMissing_Fails()
	{
		var result = CliManager.ParseMcpSolutionArgument(["--solution"], Path.GetTempPath());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("--solution");
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("A network (UNC) --solution is refused before any file-system access, as uno_app_select_solution does")]
	public void ParseMcpSolutionArgument_WhenPathIsUnc_Fails()
	{
		var result = CliManager.ParseMcpSolutionArgument(["--solution", @"\\server\share\App.slnx"], Path.GetTempPath());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("local path");
	}

	private static CliManager CreateCliManager(IWorkspaceResolver resolver)
	{
		var services = new ServiceCollection()
			.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None))
			.BuildServiceProvider();
		return new CliManager(services, new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance), resolver);
	}

	private static string CreateSolutionFile(string directory, params string[] segments)
	{
		var path = Path.Combine([directory, .. segments]);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, string.Empty);
		return path;
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"uno-cli-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}

	private sealed class CollectingLoggerProvider : ILoggerProvider
	{
		public List<string> Messages { get; } = [];

		public ILogger CreateLogger(string categoryName) => new CollectingLogger(Messages);

		public void Dispose()
		{
		}
	}

	private sealed class CollectingLogger(List<string> messages) : ILogger
	{
		private readonly List<string> _messages = messages;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			_messages.Add(formatter(state, exception));
		}
	}

	// Throws on every resolution, so RunAsync returns before starting anything; records what was asked for.
	private sealed class ThrowingWorkspaceResolver : IWorkspaceResolver
	{
		public int ResolveAsyncCallCount { get; private set; }

		public List<(string RequestedDirectory, string SolutionPath)> SolutionRequests { get; } = [];

		public Task<WorkspaceResolution> ResolveAsync(string requestedDirectory)
		{
			ResolveAsyncCallCount++;
			throw new InvalidOperationException("Workspace resolution should not be called for this command.");
		}

		public Task<WorkspaceResolution> ResolveExplicitWorkspaceAsync(string requestedDirectory)
			=> throw new NotSupportedException();

		public Task<WorkspaceResolution> ResolveSolutionAsync(string requestedDirectory, string solutionPath, WorkspaceSelectionSource selectionSource = WorkspaceSelectionSource.UserSelected)
		{
			SolutionRequests.Add((requestedDirectory, solutionPath));
			throw new InvalidOperationException("Recorded; the run stops here.");
		}
	}
}

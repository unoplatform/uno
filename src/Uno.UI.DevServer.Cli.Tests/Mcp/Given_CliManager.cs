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
	public void ParseSolutionArgument_WhenAbsent_ReturnsArgumentsUnchanged()
	{
		var result = CliManager.ParseSolutionArgument(["--port", "0", "--force-roots-fallback"], Path.GetTempPath());

		result.Success.Should().BeTrue();
		result.SolutionPath.Should().BeNull();
		result.FilteredArgs.Should().Equal("--port", "0", "--force-roots-fallback");
	}

	[TestMethod]
	[Description("--solution is consumed by the MCP bridge (resolved against the working directory) instead of being forwarded to the host, which receives the bridge's own --solution (#25524)")]
	public void ParseSolutionArgument_WhenPresent_ResolvesThePathAndRemovesItFromForwardedArguments()
	{
		var baseDirectory = CreateTempDirectory();

		try
		{
			var solutionPath = CreateSolutionFile(baseDirectory, "a", "App.slnx");

			var result = CliManager.ParseSolutionArgument(["--port", "0", "--solution", Path.Join("a", "App.slnx")], baseDirectory);

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
	[DataRow("solution={0}")]
	[DataRow("SOLUTION={0}")]
	public void ParseSolutionArgument_WhenInlineOrAlternateSpelling_ConsumesIt(string format)
	{
		var baseDirectory = CreateTempDirectory();

		try
		{
			var solutionPath = CreateSolutionFile(baseDirectory, "App.slnx");

			var result = CliManager.ParseSolutionArgument(["--port", "0", string.Format(System.Globalization.CultureInfo.InvariantCulture, format, solutionPath)], baseDirectory);

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
	public void ParseSolutionArgument_WhenNotAnExistingSolution_Fails(string fileName, bool createFile, string expectedError)
	{
		var baseDirectory = CreateTempDirectory();

		try
		{
			var path = Path.Join(baseDirectory, fileName);
			if (createFile)
			{
				File.WriteAllText(path, string.Empty);
			}

			var result = CliManager.ParseSolutionArgument(["--solution", path], baseDirectory);

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
	public void ParseSolutionArgument_WhenOptionOnlyLooksLikeSolution_ForwardsIt(string arg)
	{
		var result = CliManager.ParseSolutionArgument([arg], Path.GetTempPath());

		result.Success.Should().BeTrue();
		result.SolutionPath.Should().BeNull();
		result.FilteredArgs.Should().Equal(arg);
	}

	[TestMethod]
	[Description("--solution given twice is a usage error rather than a silent last-wins")]
	public void ParseSolutionArgument_WhenGivenTwice_Fails()
	{
		var result = CliManager.ParseSolutionArgument(["--solution", "A.slnx", "--solution=B.slnx"], Path.GetTempPath());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("more than once");
	}

	[TestMethod]
	[Description("Even when it fails, --solution is removed from the arguments: start gives the host its own --solution")]
	public void ParseSolutionArgument_WhenItFails_StillRemovesItFromForwardedArguments()
	{
		var result = CliManager.ParseSolutionArgument(["start", "--solution", "A.slnx", "--httpPort", "0", "/solution=B.slnx"], Path.GetTempPath());

		result.Success.Should().BeFalse();
		result.FilteredArgs.Should().Equal("start", "--httpPort", "0");
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

		var exitCode = await cliManager.RunAsync(["mcp", "serve", "--solution", Path.Join(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.slnx")]);

		exitCode.Should().Be(2);
		resolver.SolutionRequests.Should().BeEmpty();
		resolver.ResolveAsyncCallCount.Should().Be(0);
	}

	[TestMethod]
	[Description("start, stop, disco and health resolve the workspace from --solution, the solution the IDE opened, instead of discovering it from the folder (#26122)")]
	[DataRow("start")]
	[DataRow("stop")]
	[DataRow("disco")]
	[DataRow("health")]
	public async Task Command_WithSolution_ResolvesTheSolutionExplicitly(string command)
	{
		var directory = CreateTempDirectory();

		try
		{
			var solutionPath = CreateSolutionFile(directory, "App.slnx");
			var resolver = new ThrowingWorkspaceResolver();
			var cliManager = CreateCliManager(resolver);

			var exitCode = await cliManager.RunAsync([command, "--solution", solutionPath, "--solution-dir", directory]);

			exitCode.Should().Be(1, "the throwing resolver stops the run before anything starts");
			resolver.SolutionRequests.Should().Equal((directory, solutionPath));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	[Description("Without --solution, several solutions under one global.json are ambiguous: disco lists them and fails (#26122)")]
	public async Task DiscoJson_WhenSeveralSolutionsShareTheFolder_IsAmbiguous()
	{
		var directory = CreateTempDirectory();

		try
		{
			CreateUnoWorkspace(directory, "App.sln", "App-Copy.sln");

			var (exitCode, root) = await RunJsonAsync(["--solution-dir", directory, "disco", "--json"]);

			exitCode.Should().Be(1);
			root.GetProperty("resolutionKind").GetString().Should().Be("Ambiguous");
			root.GetProperty("errors").EnumerateArray().Select(error => error.GetString()).Should().Contain("Workspace could not be resolved.");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	[Description("With the arguments Visual Studio passes, disco and health select the --solution even when another solution shares its folder (#26122)")]
	[DataRow("disco", ".sln")]
	[DataRow("disco", ".slnx")]
	[DataRow("health", ".slnx")]
	public async Task Json_WithSolution_WhenSeveralSolutionsShareTheFolder_SelectsThatSolution(string command, string extension)
	{
		var directory = CreateTempDirectory();

		try
		{
			var solutionPath = CreateUnoWorkspace(directory, $"App{extension}", $"App-Copy{extension}")[1];

			// The exit code depends on this machine's NuGet cache, which has no package of the test's Uno SDK.
			var (_, root) = await RunJsonAsync([command, "--solution", solutionPath, "--solution-dir", directory, "--json"]);

			root.GetProperty("resolutionKind").GetString().Should().Be("CurrentDirectory");
			root.GetProperty("effectiveWorkspaceDirectory").GetString().Should().Be(directory);
			root.GetProperty("selectedSolutionPath").GetString().Should().Be(solutionPath);
			root.GetRawText().Should().NotContain("Workspace could not be resolved.").And.NotContain("WorkspaceAmbiguous");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	[Description("A --solution that is not in an Uno workspace leaves the workspace discovered from the folder, and disco warns about it")]
	public async Task DiscoJson_WithSolutionOutsideAnUnoWorkspace_WarnsAndDiscoversTheWorkspace()
	{
		var directory = CreateTempDirectory();
		var outsideDirectory = CreateTempDirectory();

		try
		{
			var solutionPath = CreateUnoWorkspace(directory, "App.sln")[0];
			var outsideSolutionPath = CreateSolutionFile(outsideDirectory, "Other.sln");

			var (_, root) = await RunJsonAsync(["disco", "--solution", outsideSolutionPath, "--solution-dir", directory, "--json"]);

			root.GetProperty("resolutionKind").GetString().Should().Be("CurrentDirectory");
			root.GetProperty("selectedSolutionPath").GetString().Should().Be(solutionPath);
			root.GetProperty("warnings").EnumerateArray().Select(warning => warning.GetString())
				.Should().ContainSingle(warning => warning!.EndsWith(CliManager.IgnoredSolutionOutcome, StringComparison.Ordinal))
				.Which.Should().Contain(outsideSolutionPath);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
			Directory.Delete(outsideDirectory, recursive: true);
		}
	}

	[TestMethod]
	[Description("Unlike MCP mode, which stops with exit code 2, disco ignores an unusable --solution with a warning and discovers the workspace from the folder")]
	[DataRow("Missing.sln", 1, "not found")]
	[DataRow("App.csproj", 1, ".sln or .slnx")]
	[DataRow("App.sln", 2, "more than once")]
	[DataRow(@"\\server\share\App.sln", 1, "--solution")]
	public async Task DiscoJson_WithUnusableSolution_WarnsAndDiscoversTheWorkspace(string solution, int count, string expectedReason)
	{
		var directory = CreateTempDirectory();

		try
		{
			var solutionPath = CreateUnoWorkspace(directory, "App.sln")[0];
			File.WriteAllText(Path.Join(directory, "App.csproj"), string.Empty);
			var value = Path.IsPathRooted(solution) ? solution : Path.Join(directory, solution);
			var solutionArgs = Enumerable.Repeat(new[] { "--solution", value }, count).SelectMany(pair => pair);

			var (exitCode, root) = await RunJsonAsync(["disco", .. solutionArgs, "--solution-dir", directory, "--json"]);

			exitCode.Should().NotBe(2);
			root.GetProperty("selectedSolutionPath").GetString().Should().Be(solutionPath);
			root.GetProperty("warnings").EnumerateArray().Select(warning => warning.GetString())
				.Should().ContainSingle(warning => warning!.EndsWith(CliManager.IgnoredSolutionOutcome, StringComparison.Ordinal))
				.Which.Should().Contain(expectedReason);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	[Description("health reports an ignored --solution as a warning")]
	public async Task HealthJson_WithUnusableSolution_ReportsAWarning()
	{
		var directory = CreateTempDirectory();

		try
		{
			CreateUnoWorkspace(directory, "App.sln");

			var (_, root) = await RunJsonAsync(["health", "--solution", Path.Join(directory, "Missing.sln"), "--solution-dir", directory, "--json"]);

			root.GetProperty("issues").EnumerateArray()
				.Should().Contain(issue =>
					issue.GetProperty("severity").GetString() == "Warning"
					&& issue.GetProperty("message").GetString()!.EndsWith(CliManager.IgnoredSolutionOutcome, StringComparison.Ordinal));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	[Description("--solution without a value is a usage error")]
	public void ParseSolutionArgument_WhenValueIsMissing_Fails()
	{
		var result = CliManager.ParseSolutionArgument(["--solution"], Path.GetTempPath());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("--solution");
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("A network (UNC) --solution is refused before any file-system access, as uno_app_select_solution does")]
	public void ParseSolutionArgument_WhenPathIsUnc_Fails()
	{
		var result = CliManager.ParseSolutionArgument(["--solution", @"\\server\share\App.slnx"], Path.GetTempPath());

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
		var path = Path.Join([directory, .. segments]);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, string.Empty);
		return path;
	}

	// A global.json declaring the Uno SDK, with the given solutions next to it.
	private static string[] CreateUnoWorkspace(string directory, params string[] solutionNames)
	{
		File.WriteAllText(Path.Join(directory, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
		return [.. solutionNames.Select(name => CreateSolutionFile(directory, name))];
	}

	// Runs the CLI with the real workspace resolver and parses the JSON it writes to stdout.
	private static async Task<(int ExitCode, JsonElement Root)> RunJsonAsync(string[] args)
	{
		var cliManager = CreateCliManager(new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance));

		using var stdout = new StringWriter();
		var previousOut = Console.Out;
		Console.SetOut(stdout);

		int exitCode;
		try
		{
			exitCode = await cliManager.RunAsync(args);
		}
		finally
		{
			Console.SetOut(previousOut);
		}

		using var document = JsonDocument.Parse(stdout.ToString());
		return (exitCode, document.RootElement.Clone());
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

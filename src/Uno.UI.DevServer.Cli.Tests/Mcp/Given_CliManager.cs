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
			await File.WriteAllTextAsync(Path.Combine(workspace, "StudioLive.slnx"), string.Empty);

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
			root.GetProperty("selectedSolutionPath").GetString().Should().Be(Path.Combine(workspace, "StudioLive.slnx"));
			root.GetProperty("resolutionKind").GetString().Should().Be("AutoDiscovered");
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

	private sealed class ThrowingWorkspaceResolver : IWorkspaceResolver
	{
		public int ResolveAsyncCallCount { get; private set; }

		public Task<WorkspaceResolution> ResolveAsync(string requestedDirectory)
		{
			ResolveAsyncCallCount++;
			throw new InvalidOperationException("Workspace resolution should not be called for this command.");
		}

		public Task<WorkspaceResolution> ResolveExplicitWorkspaceAsync(string requestedDirectory)
			=> throw new NotSupportedException();

		public Task<WorkspaceResolution> ResolveSolutionAsync(string requestedDirectory, string solutionPath, WorkspaceSelectionSource selectionSource = WorkspaceSelectionSource.UserSelected)
			=> throw new NotSupportedException();
	}
}

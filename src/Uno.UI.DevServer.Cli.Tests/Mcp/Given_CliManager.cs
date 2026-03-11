using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_CliManager
{
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

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"uno-cli-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}
}

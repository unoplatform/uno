using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_CliManager
{
	private static readonly MethodInfo _parseMcpSetupArgs = typeof(CliManager)
		.GetMethod("ParseMcpSetupArgs", BindingFlags.NonPublic | BindingFlags.Instance)!
		?? throw new InvalidOperationException("ParseMcpSetupArgs not found.");

	private static readonly MethodInfo _runMcpSubcommand = typeof(CliManager)
		.GetMethod("RunMcpSubcommand", BindingFlags.NonPublic | BindingFlags.Instance)!
		?? throw new InvalidOperationException("RunMcpSubcommand not found.");

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
	public void ParseMcpSetupArgs_MissingVersionValue_ReturnsNull()
	{
		var manager = CreateManager();

		var result = _parseMcpSetupArgs.Invoke(manager, [new[] { "--version" }, "status"]);

		result.Should().BeNull();
	}

	[TestMethod]
	public void RunMcpSubcommand_ConflictingVariantFlags_ReturnsUsageError()
	{
		var manager = CreateManager();

		var result = (int)_runMcpSubcommand.Invoke(manager, [new[] { "status", "--release", "--prerelease" }, "/project", null])!;

		result.Should().Be(2);
	}

	[TestMethod]
	public void RunMcpSubcommand_StatusUnknown_ReturnsSuccess()
	{
		var manager = CreateManager();
		var previousOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			var result = (int)_runMcpSubcommand.Invoke(manager, [new[] { "status", "unknown", "--json" }, "/project", null])!;

			result.Should().Be(0);
			writer.ToString().Should().Contain("\"callerIde\": \"unknown\"");
		}
		finally
		{
			Console.SetOut(previousOut);
		}
	}

	private static CliManager CreateManager()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IFileSystem>(new InMemoryFileSystem());
		services.AddSingleton<McpSetupOrchestrator>();

		var provider = services.BuildServiceProvider();
		return new CliManager(
			provider,
			new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance));
	}
}

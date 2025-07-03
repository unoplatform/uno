using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class DevServerTests
{
	private static ILogger<DevServerTestHelper> _logger = null!;

	[ClassInitialize]
	public static void ClassInitialize(TestContext context)
	{
		// Create a logger factory and logger
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.AddDebug();
		});

		_logger = loggerFactory.CreateLogger<DevServerTestHelper>();
	}

	public TestContext? TestContext { get; set; }

	private CancellationToken CT => TestContext?.CancellationTokenSource.Token ?? CancellationToken.None;

	[TestMethod]
	public async Task DevServer_ShouldStart()
	{
		// Arrange
		await using var helper = new DevServerTestHelper(_logger);

		try
		{
			// Act
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			// Assert
			started.Should().BeTrue("dev server should start successfully");
			helper.AssertRunning();
			helper.AssertConsoleOutputContains("Application started");
		}
		finally
		{
			// Cleanup
			await helper.StopAsync(CT);
		}
	}

	[TestMethod]
	public async Task DevServer_ShouldCaptureOutput()
	{
		// Arrange
		await using var helper = new DevServerTestHelper(_logger, environmentVariables: _telemetryOptOutVariables);

		try
		{
			// Act
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			// Assert
			started.Should().BeTrue("dev server should start successfully");
			helper.ConsoleOutput.Should().NotBeEmpty("dev server should produce console output");

			// The following assertions depend on the actual output of the dev server
			// and may need to be adjusted based on the actual output
			helper.AssertConsoleOutputContains("Application started");
			helper.AssertConsoleOutputContains("Now listening on:");
		}
		finally
		{
			// Cleanup
			await helper.StopAsync(CT);
		}
	}

	private readonly IReadOnlyDictionary<string, string>? _telemetryOptOutVariables =
		new Dictionary<string, string>() { { "UNO_PLATFORM_TELEMETRY_OPTOUT ", "true" } };

	[TestMethod]
	public async Task DevServer_ShouldStopCleanly()
	{
		// Arrange
		await using var helper = new DevServerTestHelper(_logger);

		// Act
		var started = await helper.StartAsync(CT);
		helper.EnsureStarted();

		await helper.StopAsync(CT);

		// Assert
		started.Should().BeTrue("dev server should start successfully");
		helper.IsRunning.Should().BeFalse("dev server should not be running after stopping");
	}

	[TestMethod]
	public async Task DevServer_ShouldHandleMultipleStartStopCycles()
	{
		// Arrange
		await using var helper = new DevServerTestHelper(_logger);

		// Act & Assert - First cycle
		var started1 = await helper.StartAsync(CT);
		helper.EnsureStarted();

		started1.Should().BeTrue("dev server should start successfully on first attempt");
		await helper.StopAsync(CT);
		helper.IsRunning.Should().BeFalse("dev server should not be running after first stop");

		// Act & Assert - Second cycle
		var started2 = await helper.StartAsync(CT);
		started2.Should().BeTrue("dev server should start successfully on second attempt");
		await helper.StopAsync(CT);
		helper.IsRunning.Should().BeFalse("dev server should not be running after second stop");
	}

	[TestMethod]
	public async Task DevServer_ShouldHandleDispose()
	{
		// Arrange
		DevServerTestHelper helper;

		// Act
		await using (helper = new DevServerTestHelper(_logger))
		{
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			started.Should().BeTrue("dev server should start successfully");
		} // Dispose happens here

		// Assert
		// We can't access helper.IsRunning after disposal, but we can check that the process is no longer running
		// by trying to start a new server on the same port
		await using (var helper2 = new DevServerTestHelper(_logger))
		{
			var started = await helper2.StartAsync(CT);
			helper2.EnsureStarted();

			started.Should().BeTrue("dev server should start successfully after previous instance was disposed");
			await helper2.StopAsync(CT);
		}
	}
}

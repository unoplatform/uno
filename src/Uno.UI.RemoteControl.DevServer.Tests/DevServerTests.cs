using Uno.UI.RemoteControl.DevServer.Tests.Helpers;

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
	[Retry(2, MillisecondsDelayBetweenRetries = 1000)]
	[Description("Intermittent: random port binding may collide or the host process may take longer to start on CI")]
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
			helper.AssertConsoleOutputContains("Now listening on:");
		}
		finally
		{
			// Cleanup
			await helper.StopAsync(CT);
		}
	}

	[TestMethod]
	[Retry(2, MillisecondsDelayBetweenRetries = 1000)]
	[Description("Intermittent: random port binding may collide or the host process may take longer to start on CI")]
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
	[Retry(2, MillisecondsDelayBetweenRetries = 1000)]
	[Description("Intermittent: random port binding may collide or the host process may take longer to start on CI")]
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
}

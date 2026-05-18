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
		new Dictionary<string, string>() { { "UNO_PLATFORM_TELEMETRY_OPTOUT", "true" } };

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

	[TestMethod]
	[Retry(2, MillisecondsDelayBetweenRetries = 1000)]
	[Description("Regression for #23287: host must satisfy an add-in's older-major AssemblyRefs to shared-framework OOB packages (e.g. System.Text.Encodings.Web 8.0.0.0 in a net8.0 add-in) from its own newer-major loaded instance — without throwing FileNotFoundException during startup.")]
	public async Task DevServer_ShouldStart_WithAddInThatHasCrossMajorVersionFrameworkRefs()
	{
		// The fixture is built as a net8.0 class library that touches
		// JavaScriptEncoder.Default in a static cctor (see
		// Fixtures/AddInWithCrossMajorVersionRefs). Its compiled AssemblyRef to
		// System.Text.Encodings.Web embeds at v8.0.0.0, which won't strict-match
		// the v9/v10 the host has loaded from its shared framework. Without the
		// AddInLoadContext + Default.Resolving bridging in
		// Uno.UI.RemoteControl.Host this manifests as the original client crash
		// (KiotaJsonSerializationContext..cctor → FNF Encodings.Web 8.0.0.0).
		var testAssemblyDir = Path.GetDirectoryName(typeof(DevServerTests).Assembly.Location)!;
		var fixtureDll = Path.Combine(
			testAssemblyDir,
			"Fixtures",
			"AddInWithCrossMajorVersionRefs",
			"AddInWithCrossMajorVersionRefs.dll");

		File.Exists(fixtureDll).Should()
			.BeTrue("test fixture DLL must be copied to test output by the _CopyAddInTestFixtures target");

		await using var helper = new DevServerTestHelper(_logger);

		try
		{
			var started = await helper.StartAsync(CT, extraArgs: $"--addins \"{fixtureDll}\"");
			helper.EnsureStarted();

			started.Should().BeTrue("dev server should start successfully with the cross-major-version add-in loaded");
			helper.AssertRunning();
			helper.AssertConsoleOutputContains("Now listening on:");

			// Frame the negative assertion by class-of-bug, not by today's specific
			// stack — keeps the test relevant as host TFMs roll forward.
			helper.ConsoleOutput
				.Should().NotMatchRegex(
					@"Unhandled exception[\s\S]*FileNotFoundException[\s\S]*System\.Text\.Encodings\.Web",
					because: "host must bridge cross-major-version AssemblyRefs from add-ins via Default.Resolving instead of crashing");
		}
		finally
		{
			await helper.StopAsync(CT);
		}
	}
}

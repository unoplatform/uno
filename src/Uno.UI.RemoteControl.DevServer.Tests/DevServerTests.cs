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
	[Description("Regression for #23287: the host's add-in load pipeline must boot cleanly when an add-in carries older-major AssemblyRefs to shared-framework OOB packages (e.g. System.Text.Encodings.Web 8.0.0.0 in a net8.0 add-in) AND is activated through the [ServiceCollectionExtension] attribute scan that the host runs at startup.")]
	public async Task DevServer_ShouldStart_WithAddInThatHasCrossMajorVersionFrameworkRefs()
	{
		// The fixture is built as a net8.0 class library that touches
		// JavaScriptEncoder.Default and IServiceCollection in a
		// [ServiceCollectionExtension]-registered ctor (see
		// Fixtures/AddInWithCrossMajorVersionRefs). The host's
		// AddFromServiceExtensionAttributes invokes that ctor through
		// Activator.CreateInstance, so the fixture's compiled v8.0.0.0
		// AssemblyRefs are actually resolved — exercising the AddInLoadContext
		// load path and the AssemblyLoadContext.Default.Resolving handler in
		// Uno.UI.RemoteControl.Host rather than sitting dormant in metadata.
		//
		// This is a positive-outcome regression test: depending on the .NET
		// runtime version, the binder may also successfully lax-bind these refs
		// even without the host's fix, so a "must throw FileNotFoundException
		// without the fix" assertion isn't deterministic across runtimes. The
		// failure modes this catches are the ones that WOULD remain regardless
		// of runtime laxness: a fatal crash in add-in load, the ctor never being
		// invoked at all (broken attribute scan), or the host failing to reach
		// the listening state. The end-to-end repro in
		// D:\Dev\Uno-Pocs\DevServerLicensingRepro\ covers the real Kiota /
		// Uno.Settings.DevServer path on a runtime that does still strict-bind.
		var testAssemblyDir = Path.GetDirectoryName(typeof(DevServerTests).Assembly.Location)!;
		var fixtureDll = Path.Combine(
			testAssemblyDir,
			"Fixtures",
			"AddInWithCrossMajorVersionRefs",
			"AddInWithCrossMajorVersionRefs.dll");

		File.Exists(fixtureDll).Should()
			.BeTrue("test fixture DLL must be copied to test output by the _BuildAndCopyAddInTestFixtures target");

		await using var helper = new DevServerTestHelper(_logger);

		try
		{
			var started = await helper.StartAsync(CT, extraArgs: $"--addins \"{fixtureDll}\"");
			helper.EnsureStarted();

			started.Should().BeTrue("dev server should start successfully with the cross-major-version add-in loaded");
			helper.AssertRunning();
			helper.AssertConsoleOutputContains("Now listening on:");
			helper.AssertConsoleOutputContains("Loading add-in assembly");

			// Runtime-stable assertion: a fatal CLR "Unhandled exception" banner
			// terminates the process before "Now listening on:" is reached, so
			// rather than substring-matching on the phrase (which could match a
			// non-fatal logger message containing the literal text) we just
			// confirm the host process is still alive and has actually reached
			// the listening state. If the add-in load pipeline regresses in a
			// way that crashes startup, this combination of asserts fails.
			helper.IsRunning.Should().BeTrue("host process must still be alive after the listening-state log");
		}
		finally
		{
			await helper.StopAsync(CT);
		}
	}
}

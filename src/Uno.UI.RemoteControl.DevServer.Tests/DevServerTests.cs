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

	// ------------------------------------------------------------------ DI cross-ALC baseline

	[TestMethod]
	[Retry(2, MillisecondsDelayBetweenRetries = 1000)]
	[Description("Baseline: an add-in loaded into AddInLoadContext must be able to register services via [ServiceCollectionExtension], proving IServiceCollection bridges to the host's instance.")]
	public async Task DevServer_ShouldInvokeServiceRegistrationCtor_WhenAddInDeclaresServiceCollectionExtension()
	{
		var testAssemblyDir = Path.GetDirectoryName(typeof(DevServerTests).Assembly.Location)!;
		var fixtureDll = Path.Combine(
			testAssemblyDir, "Fixtures", "AddInWithDiServiceRegistration", "AddInWithDiServiceRegistration.dll");

		File.Exists(fixtureDll).Should()
			.BeTrue("DI service-registration fixture must be staged by _BuildAndCopyAddInTestFixtures");

		var ctorSentinel = Path.Combine(Path.GetTempPath(), $"di-ctor-{Guid.NewGuid():N}.txt");
		try
		{
			await using var helper = new DevServerTestHelper(
				_logger,
				environmentVariables: new Dictionary<string, string>
				{
					["UNO_DEVSERVER_TEST_DI_CTOR_SENTINEL"] = ctorSentinel,
				});

			try
			{
				var started = await helper.StartAsync(CT, extraArgs: $"--addins \"{fixtureDll}\"");
				helper.EnsureStarted();

				started.Should().BeTrue("dev server should start with the DI fixture add-in loaded");
				helper.AssertRunning();

				// The fixture's ServicesRegistration.ctor writes this sentinel when called.
				// If IServiceCollection didn't bridge to the host's instance, Activator.CreateInstance
				// would throw ArgumentException and the host would log the error without invoking the ctor.
				File.Exists(ctorSentinel).Should().BeTrue(
					"ServicesRegistration.ctor must have been invoked, proving IServiceCollection Type-identity across the ALC boundary");
			}
			finally
			{
				await helper.StopAsync(CT);
			}
		}
		finally
		{
			if (File.Exists(ctorSentinel))
			{
				File.Delete(ctorSentinel);
			}
		}
	}

	[TestMethod]
	[Retry(2, MillisecondsDelayBetweenRetries = 1000)]
	[Description("Baseline: an add-in's IHostedService must be started by ASP.NET Core's host, and the hosted service must be able to resolve add-in-defined types from the shared IServiceProvider.")]
	public async Task DevServer_ShouldStartHostedService_WhenAddInRegistersIHostedService()
	{
		var testAssemblyDir = Path.GetDirectoryName(typeof(DevServerTests).Assembly.Location)!;
		var fixtureDll = Path.Combine(
			testAssemblyDir, "Fixtures", "AddInWithDiServiceRegistration", "AddInWithDiServiceRegistration.dll");

		File.Exists(fixtureDll).Should()
			.BeTrue("DI service-registration fixture must be staged by _BuildAndCopyAddInTestFixtures");

		var hostedSentinel = Path.Combine(Path.GetTempPath(), $"di-hosted-{Guid.NewGuid():N}.txt");
		try
		{
			await using var helper = new DevServerTestHelper(
				_logger,
				environmentVariables: new Dictionary<string, string>
				{
					["UNO_DEVSERVER_TEST_DI_HOSTED_SENTINEL"] = hostedSentinel,
				});

			try
			{
				var started = await helper.StartAsync(CT, extraArgs: $"--addins \"{fixtureDll}\"");
				helper.EnsureStarted();

				started.Should().BeTrue("dev server should start with the DI fixture add-in loaded");
				helper.AssertRunning();

				// The fixture's TestHostedService.StartAsync writes the count of ITestToken
				// registrations to this sentinel.  Proving:
				//   (a) IHostedService bridged correctly (service was picked up and started by ASP.NET Core)
				//   (b) IServiceProvider bridged correctly (service received the host's provider)
				//   (c) ServiceDescriptors survive the ALC boundary (GetServices<ITestToken> finds 2)
				File.Exists(hostedSentinel).Should().BeTrue(
					"TestHostedService.StartAsync must have been invoked, proving IHostedService and IServiceProvider Type-identity across the ALC boundary");

				var countText = File.ReadAllText(hostedSentinel).Trim();
				int.TryParse(countText, out var tokenCount).Should().BeTrue("sentinel must contain a numeric token count");
				tokenCount.Should().Be(2,
					"ServicesRegistration.ctor registered exactly 2 ITestToken implementations; GetServices must find both");
			}
			finally
			{
				await helper.StopAsync(CT);
			}
		}
		finally
		{
			if (File.Exists(hostedSentinel))
			{
				File.Delete(hostedSentinel);
			}
		}
	}

	// ------------------------------------------------------------------ cross-add-in type sharing regression (#23304)

	[TestMethod]
	[Retry(2, MillisecondsDelayBetweenRetries = 1000)]
	[Description("Regression for #23304: when two add-ins share a contract assembly (e.g. Uno.Licensing.Sdk.Contracts) " +
		"that is physically present only in the provider's directory and absent from every add-in's .deps.json, " +
		"the file-system probe (step 4) in AddInLoadContext.Load must locate the DLL, load it once into the shared " +
		"AddInLoadContext, and give both add-ins a single ILicensingTestContract Type identity so DI resolution succeeds.")]
	public async Task DevServer_ShouldResolveSharedContractAcrossAddIns_WhenContractOnlyInProviderDirectory()
	{
		var testAssemblyDir = Path.GetDirectoryName(typeof(DevServerTests).Assembly.Location)!;
		var providerDll = Path.Combine(
			testAssemblyDir, "Fixtures", "AddInWithSharedContractProvider", "AddInWithSharedContractProvider.dll");
		var consumerDll = Path.Combine(
			testAssemblyDir, "Fixtures", "AddInWithSharedContractConsumer", "AddInWithSharedContractConsumer.dll");

		File.Exists(providerDll).Should()
			.BeTrue("provider fixture must be staged by _BuildAndCopyAddInTestFixtures");
		File.Exists(consumerDll).Should()
			.BeTrue("consumer fixture must be staged by _BuildAndCopyAddInTestFixtures");

		// The contracts DLL must be in the provider's directory (the fix loads it from there).
		var contractsDll = Path.Combine(
			testAssemblyDir, "Fixtures", "AddInWithSharedContractProvider", "Uno.Licensing.TestContracts.dll");
		File.Exists(contractsDll).Should()
			.BeTrue("Uno.Licensing.TestContracts.dll must be in the provider's fixture directory (Private=true on the ProjectReference)");

		// The contracts DLL must NOT be in the consumer's directory (simulates the real scenario).
		var consumerContractsDll = Path.Combine(
			testAssemblyDir, "Fixtures", "AddInWithSharedContractConsumer", "Uno.Licensing.TestContracts.dll");
		File.Exists(consumerContractsDll).Should()
			.BeFalse("Uno.Licensing.TestContracts.dll must NOT be in the consumer's fixture directory (Private=false on the ProjectReference)");

		var sentinel = Path.Combine(Path.GetTempPath(), $"shared-contract-{Guid.NewGuid():N}.txt");
		try
		{
			await using var helper = new DevServerTestHelper(
				_logger,
				environmentVariables: new Dictionary<string, string>
				{
					["UNO_DEVSERVER_TEST_SHARED_CONTRACT_SENTINEL"] = sentinel,
				});

			try
			{
				// Host parses --addins as a semicolon-separated path list (see
				// Program.cs and ConfigurationExtensions.GetAddinsValue). Passing two
				// space-separated paths would leave Microsoft.Extensions.Configuration
				// to drop the second one as an unmatched positional argument.
				var started = await helper.StartAsync(
					CT,
					extraArgs: $"--addins \"{providerDll};{consumerDll}\"");
				helper.EnsureStarted();

				started.Should().BeTrue("dev server should start with both cross-add-in fixtures loaded");
				helper.AssertRunning();

				// If the fix works: AddInLoadContext.Load's file-system probe (step 4) finds
				// Uno.Licensing.TestContracts.dll in the provider's directory and loads it
				// once into the shared AddInLoadContext. Both add-ins then see the same Type
				// identity; the consumer's IHostedService receives the ILicensingTestContract
				// instance registered by the provider and writes its AssemblyQualifiedName
				// to the sentinel file.
				//
				// Without the fix: FileNotFoundException or InvalidOperationException during
				// add-in activation prevents the sentinel from being written.
				File.Exists(sentinel).Should().BeTrue(
					"ConsumerHostedService.StartAsync must have been invoked with ILicensingTestContract " +
					"resolved from DI, proving cross-add-in type sharing works when the contract DLL is " +
					"only in the provider's directory");

				// Stronger assertion: the sentinel contains the AQN of the contract type the
				// consumer actually received from DI. If a regression left two distinct Type
				// instances of ILicensingTestContract in play, DI would have failed before
				// StartAsync ran — but as a belt-and-braces check, confirm the AQN refers to
				// the shared contracts assembly rather than some unrelated fallback string.
				var sentinelContent = File.ReadAllText(sentinel).Trim();
				sentinelContent.Should().Contain(
					"Uno.Licensing.TestContracts",
					"the resolved contract type must come from the shared contracts assembly");
				sentinelContent.Should().NotBe(
					"resolved",
					"the consumer must have received a concrete ILicensingTestContract instance whose AQN " +
					"identifies the contracts assembly, not the placeholder string written when AQN was null");
			}
			finally
			{
				await helper.StopAsync(CT);
			}
		}
		finally
		{
			if (File.Exists(sentinel))
			{
				File.Delete(sentinel);
			}
		}
	}

	// ------------------------------------------------------------------ cross-major version regression (#23287)

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
		var testAssemblyDir = Path.GetDirectoryName(typeof(DevServerTests).Assembly.Location)!;
		var fixtureDll = Path.Combine(
			testAssemblyDir,
			"Fixtures",
			"AddInWithCrossMajorVersionRefs",
			"AddInWithCrossMajorVersionRefs.dll");

		File.Exists(fixtureDll).Should()
			.BeTrue("test fixture DLL must be copied to test output by the _BuildAndCopyAddInTestFixtures target");

		var versionSentinel = Path.Combine(
			Path.GetTempPath(), $"encodingsweb-version-{Guid.NewGuid():N}.txt");
		try
		{
			await using var helper = new DevServerTestHelper(
				_logger,
				environmentVariables: new Dictionary<string, string>
				{
					["UNO_DEVSERVER_TEST_ENCODINGSWEB_VERSION_PATH"] = versionSentinel,
				});

			try
			{
				var started = await helper.StartAsync(CT, extraArgs: $"--addins \"{fixtureDll}\"");
				helper.EnsureStarted();

				started.Should().BeTrue("dev server should start successfully with the cross-major-version add-in loaded");
				helper.AssertRunning();
				helper.AssertConsoleOutputContains("Now listening on:");

				// Strong assertion: the fixture writes the actual version of
				// System.Text.Encodings.Web that was resolved at runtime. If the
				// bridge is working, the host's loaded version (major >= 9) is
				// returned even though the fixture compiled against v8.0.0.0.
				// Without the fix a strict-binding runtime would throw and the
				// sentinel would never be written (ctor never called).
				File.Exists(versionSentinel).Should().BeTrue(
					"the fixture's ctor must have been invoked and the " +
					"System.Text.Encodings.Web version must have been written");

				var versionText = File.ReadAllText(versionSentinel).Trim();
				Version.TryParse(versionText, out var resolvedVersion).Should().BeTrue(
					"sentinel must contain a valid version string, got: '{0}'", versionText);
				resolvedVersion!.Major.Should().BeGreaterThanOrEqualTo(9,
					"the bridge must return the host's loaded version (>= 9), not the " +
					"fixture's compiled v8.0.0.0 reference");

				helper.IsRunning.Should().BeTrue(
					"host process must still be alive after the listening-state log");
			}
			finally
			{
				await helper.StopAsync(CT);
			}
		}
		finally
		{
			if (File.Exists(versionSentinel))
			{
				File.Delete(versionSentinel);
			}
		}
	}
}

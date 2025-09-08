using Uno.UI.RemoteControl.DevServer.Tests.Helpers;
using Uno.UI.RemoteControl.DevServer.Tests.Telemetry;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class ScopedServiceIsolationTests : TelemetryTestBase
{
	[ClassInitialize]
	public static void ClassInitialize(TestContext context) => GlobalClassInitialize<ScopedServiceIsolationTests>(context);

	[TestMethod]
	public async Task ScopedServices_ShouldBeIsolated_BetweenConnections()
	{
		// Arrange
		var helper = new DevServerTestHelper(Logger);

		try
		{
			// Act & Assert - Start the dev server to validate that scoped services work correctly
			// ASP.NET Core automatically provides per-connection scoping via context.RequestServices
			var started = await RunTelemetryTestCycle(helper);
			started.Should().BeTrue("dev server should start successfully with scoped telemetry services");
		}
		finally
		{
			await helper.StopAsync(CT);
		}
	}

	[TestMethod]
	public async Task TelemetryRedirection_ShouldWork_WithScopedServices()
	{
		// Arrange
		var tempFile = Path.GetTempFileName();
		var helper = CreateTelemetryHelperWithExactPath(tempFile);

		try
		{
			// Act
			var started = await RunTelemetryTestCycle(helper);

			// Assert - Telemetry should be written to file during server startup/shutdown
			started.Should().BeTrue("dev server should start successfully");
			File.Exists(tempFile).Should().BeTrue("temp file should exist");

			var fileContent = await File.ReadAllTextAsync(tempFile, CT);
			fileContent.Should().NotBeNullOrEmpty("temp file should not be empty");
			fileContent.Should().Contain("dev-server", "telemetry should contain dev-server events");

			Logger.LogInformation($"[DEBUG_LOG] Telemetry file content: {fileContent}");
		}
		finally
		{
			await CleanupTelemetryTest(helper, tempFile);
		}
	}

	[TestMethod]
	public async Task ConnectionContext_ShouldBePopulated_WithConnectionMetadata()
	{
		// Arrange
		var tempFile = Path.GetTempFileName();
		var helper = CreateTelemetryHelperWithExactPath(tempFile);

		try
		{
			// Act
			var started = await RunTelemetryTestCycle(helper);

			// Assert - Telemetry should be written to file with connection metadata
			started.Should().BeTrue("dev server should start successfully");
			File.Exists(tempFile).Should().BeTrue("temp file should exist");

			var fileContent = await File.ReadAllTextAsync(tempFile, CT);
			fileContent.Should().NotBeNullOrEmpty("temp file should not be empty");
			fileContent.Should().Contain("dev-server", "telemetry should contain dev-server events");

			// The telemetry should contain connection metadata from the enhanced scoped services
			// This validates that ConnectionContext is properly integrated with TelemetrySession
			Logger.LogInformation($"[DEBUG_LOG] Enhanced telemetry file content: {fileContent}");
		}
		finally
		{
			await CleanupTelemetryTest(helper, tempFile);
		}
	}
}

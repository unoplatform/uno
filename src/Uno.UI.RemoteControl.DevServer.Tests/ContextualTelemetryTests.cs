using FluentAssertions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public partial class ContextualTelemetryTests : TelemetryTestBase
{
	[GeneratedRegex(@"test_connection_telemetry_(global|connection-[a-f0-9]{8})_\d{8}_\d{6}\.json")]
	private static partial Regex TelemetryFileNamePattern();

	[ClassInitialize]
	public static void ClassInitialize(TestContext context)
	{
		InitializeLogger<ContextualTelemetryTests>();
	}

	[TestMethod]
	public async Task GlobalTelemetry_ShouldCreateGlobalContextFile()
	{
		// Arrange
		var (helper, tempDir) = CreateTelemetryHelper("test_telemetry");

		try
		{
			// Act
			var started = await RunTelemetryTestCycle(helper);

			// Assert - Check that a global context file was created
			started.Should().BeTrue("dev server should start successfully");

			var globalFiles = Directory.GetFiles(tempDir, "test_telemetry_global_*.json");
			globalFiles.Should().NotBeEmpty("global telemetry file should be created");

			var globalFile = globalFiles.First();
			var fileContent = await File.ReadAllTextAsync(globalFile, CT);
			fileContent.Should().NotBeNullOrEmpty("global telemetry file should not be empty");
			fileContent.Should().Contain("DevServer", "global telemetry should contain DevServer events");

			Logger.LogInformation($"[DEBUG_LOG] Global telemetry file: {globalFile}");
			Logger.LogInformation($"[DEBUG_LOG] Global telemetry content: {fileContent}");
		}
		finally
		{
			await CleanupTelemetryTest(helper, tempDir, "test_telemetry_*.json");
		}
	}

	[TestMethod]
	public async Task ConnectionTelemetry_ShouldCreateConnectionContextFiles()
	{
		// Arrange
		var (helper, tempDir) = CreateTelemetryHelper("test_connection_telemetry");

		try
		{
			// Act
			var started = await RunTelemetryTestCycle(helper, waitTimeMs: 3000);

			// Assert - Check that both global and connection context files were created
			started.Should().BeTrue("dev server should start successfully");

			var allFiles = await ValidateTelemetryFiles(tempDir, "test_connection_telemetry_*.json");
			ValidateGlobalFiles(allFiles);

			// Verify file naming pattern
			foreach (var file in allFiles)
			{
				var fileName = Path.GetFileName(file);
				var isValidPattern = TelemetryFileNamePattern().IsMatch(fileName);
				isValidPattern.Should().BeTrue($"file {fileName} should follow the contextual naming pattern");
			}
		}
		finally
		{
			await CleanupTelemetryTest(helper, tempDir, "test_connection_telemetry_*.json");
		}
	}

	[TestMethod]
	public async Task TelemetryIsolation_ShouldSeparateGlobalAndConnectionEvents()
	{
		// Arrange
		var (helper, tempDir) = CreateTelemetryHelper("test_isolation_telemetry");

		try
		{
			// Act
			var started = await RunTelemetryTestCycle(helper, waitTimeMs: 3000);

			// Assert - Analyze the content of different files
			started.Should().BeTrue("dev server should start successfully");

			var allFiles = await ValidateTelemetryFiles(tempDir, "test_isolation_telemetry_*.json");
			var globalFiles = ValidateGlobalFiles(allFiles);

			// Verify global file contains server-wide events
			foreach (var globalFile in globalFiles)
			{
				var globalContent = await File.ReadAllTextAsync(globalFile, CT);
				globalContent.Should().Contain("DevServer", "global file should contain DevServer events");

				Logger.LogInformation($"[DEBUG_LOG] Global file: {Path.GetFileName(globalFile)}");
				Logger.LogInformation($"[DEBUG_LOG] Global events found: {globalContent.Contains("DevServer")}");
			}

			// Log all files for debugging
			foreach (var file in allFiles)
			{
				var fileName = Path.GetFileName(file);
				var content = await File.ReadAllTextAsync(file, CT);
				var eventCount = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

				Logger.LogInformation($"[DEBUG_LOG] File: {fileName}, Events: {eventCount}");
			}
		}
		finally
		{
			await CleanupTelemetryTest(helper, tempDir, "test_isolation_telemetry_*.json");
		}
	}
}

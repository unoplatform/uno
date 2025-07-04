using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class ContextualNamingIntegrationTests : TelemetryTestBase
{
	[ClassInitialize]
	public static void ClassInitialize(TestContext context)
	{
		InitializeLogger<ContextualNamingIntegrationTests>();
	}

	[TestMethod]
	public async Task ContextualNaming_ShouldCreateSeparateFiles_WhenNoExtensionProvided()
	{
		// Arrange
		var (helper, tempDir) = CreateTelemetryHelper("test_contextual_naming"); // No extension

		try
		{
			// Act
			var started = await RunTelemetryTestCycle(helper, waitTimeMs: 3000);

			// Assert - Check that contextual files were created
			started.Should().BeTrue("dev server should start successfully");

			var allFiles = await ValidateTelemetryFiles(tempDir, "test_contextual_naming_*.json", shouldContainDevServer: false);
			ValidateGlobalFiles(allFiles);

			// Verify file naming pattern
			foreach (var file in allFiles)
			{
				var fileName = Path.GetFileName(file);
				var isGlobalFile = fileName.Contains("_global_");
				var isConnectionFile = fileName.Contains("_connection-");

				(isGlobalFile || isConnectionFile).Should().BeTrue($"file {fileName} should be either global or connection file");

				// Verify content based on file type
				if (isGlobalFile)
				{
					var fileContent = await File.ReadAllTextAsync(file, CT);
					fileContent.Should().Contain("DevServer", "global file should contain DevServer events");
				}
			}

			// Verify that we have separate files for different contexts
			allFiles.Length.Should().BeGreaterOrEqualTo(1, "should have at least one telemetry file");
		}
		finally
		{
			await CleanupTelemetryTest(helper, tempDir, "test_contextual_naming_*.json");
		}
	}

	[TestMethod]
	public async Task BackwardCompatibility_ShouldUseExactPath_WhenExtensionProvided()
	{
		// Arrange
		var tempDir = Path.GetTempPath();
		var exactFilePath = Path.Combine(tempDir, "test_exact_path.json"); // With extension
		var helper = CreateTelemetryHelperWithExactPath(exactFilePath);

		try
		{
			// Act
			var started = await RunTelemetryTestCycle(helper, waitTimeMs: 3000);

			// Assert - Check that the exact file was created (backward compatibility)
			started.Should().BeTrue("dev server should start successfully");
			File.Exists(exactFilePath).Should().BeTrue("exact telemetry file should be created for backward compatibility");

			var fileContent = await File.ReadAllTextAsync(exactFilePath, CT);
			fileContent.Should().NotBeNullOrEmpty("exact telemetry file should not be empty");
			fileContent.Should().Contain("DevServer", "file should contain DevServer events");

			// Verify no contextual files were created
			var contextualFiles = Directory.GetFiles(tempDir, "test_exact_path_*.json");
			contextualFiles.Should().BeEmpty("no contextual files should be created when exact path is provided");

			Logger.LogInformation($"[DEBUG_LOG] Exact file created: {Path.GetFileName(exactFilePath)}");
			Logger.LogInformation($"[DEBUG_LOG] Content length: {fileContent.Length}");
		}
		finally
		{
			await CleanupTelemetryTest(helper, exactFilePath);
		}
	}
}

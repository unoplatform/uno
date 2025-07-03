using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class TelemetryRedirectionTests
{
	private static ILogger<TelemetryRedirectionTests> _logger = null!;

	public TestContext? TestContext { get; set; }

	private CancellationToken CT => TestContext?.CancellationTokenSource.Token ?? CancellationToken.None;

	[ClassInitialize]
	public static void ClassInitialize(TestContext context)
	{
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.AddDebug();
		});

		_logger = loggerFactory.CreateLogger<TelemetryRedirectionTests>();
	}

	[TestMethod]
	public async Task TelemetryRedirection_ShouldWriteToFile_WhenEnvironmentVariableIsSet()
	{
		// Arrange
		var tempFile = Path.GetTempFileName();
		var helper = new DevServerTestHelper(
			_logger,
			environmentVariables: new Dictionary<string, string>()
			{
				{ "UNO_DEVSERVER_TELEMETRY_FILE", tempFile }
			});
		try
		{
			// Act
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			await Task.Delay(5000, CT);

			// Assert - Ensure file is create with content...
			File.Exists(tempFile).Should().BeTrue("temp file should exist");
			var fileContent = await File.ReadAllTextAsync(tempFile, CT);
			fileContent.Should().NotBeNullOrEmpty("temp file should not be empty");
		}
		finally
		{
			// Cleanup
			await helper.StopAsync(CT);
			File.Delete(tempFile);
		}
	}
}

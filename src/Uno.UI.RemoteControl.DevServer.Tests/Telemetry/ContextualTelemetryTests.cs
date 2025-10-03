namespace Uno.UI.RemoteControl.DevServer.Tests.Telemetry;

[TestClass]
public class TelemetryContextualTests : TelemetryTestBase
{
	[ClassInitialize]
	public static void ClassInitialize(TestContext context) => GlobalClassInitialize<TelemetryContextualTests>(context);

	[TestMethod]
	public async Task Telemetry_GlobalOnly_WhenNoClient()
	{
		// Arrange
		var fileName = GetTestTelemetryFileName("globalonly");
		var (helper, tempDir) = CreateTelemetryHelper(fileName);

		try
		{
			// Act
			var started = await RunTelemetryTestCycle(helper, 3000);
			var filePath = Path.Combine(tempDir, fileName);
			var fileExists = File.Exists(filePath);
			var fileContent = fileExists ? await File.ReadAllTextAsync(filePath, CT) : string.Empty;
			var events = fileContent.Length > 0 ? ParseTelemetryEvents(fileContent) : new();
			WriteEventsList(events);

			// Assert
			started.Should().BeTrue();
			fileExists.Should().BeTrue();
			events.Should().NotBeEmpty();
			AssertHasPrefix(events, "global");
			events.All(e => e.Prefix == "global").Should().BeTrue();
		}
		finally
		{
			await CleanupTelemetryTest(helper, tempDir, fileName);
		}
	}
}

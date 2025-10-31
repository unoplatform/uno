using Uno.UI.RemoteControl.DevServer.Tests.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Telemetry;

[TestClass]
public class TelemetryServerTests : TelemetryTestBase
{
	[ClassInitialize]
	public static void ClassInitialize(TestContext context) => GlobalClassInitialize<TelemetryServerTests>(context);

	[TestMethod]
	public async Task Telemetry_Server_LogsConnectionEvents()
	{
		var solution = SolutionHelper;

		// Arrange
		var fileName = GetTestTelemetryFileName("serverconn");
		var tempDir = Path.GetTempPath();
		var filePath = Path.Combine(tempDir, fileName);
		await solution.CreateSolutionFileAsync();
		await using var helper = CreateTelemetryHelperWithExactPath(filePath, solutionPath: solution.SolutionFile);

		try
		{
			// Act
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();
			var client = RemoteControlClient.Initialize(
				typeof(object),
				new[] { new ServerEndpointAttribute("localhost", helper.Port) }
			);
			await client.SendMessage(new KeepAliveMessage());
			await Task.Delay(1000, CT);
			await helper.AttemptGracefulShutdownAsync(CT);
			var fileExists = File.Exists(filePath);
			var fileContent = fileExists ? await File.ReadAllTextAsync(filePath, CT) : string.Empty;
			var events = fileContent.Length > 0 ? ParseTelemetryEvents(fileContent) : new();
			WriteEventsList(events);

			// Assert
			started.Should().BeTrue();
			fileExists.Should().BeTrue();
			events.Should().NotBeEmpty();
			AssertHasPrefix(events, "global");
			events.Any(e => e.Prefix.StartsWith("connection-")).Should().BeTrue();
		}
		finally
		{
			await helper.StopAsync(CT);
			if (File.Exists(filePath))
			{
				try { File.Delete(filePath); }
				catch { }
			}
		}
	}

	[TestMethod]
	public async Task Telemetry_FileTelemetry_AppliesEventsPrefix()
	{
		var solution = SolutionHelper;

		// Arrange
		var fileName = GetTestTelemetryFileName("eventsprefix");
		var tempDir = Path.GetTempPath();
		var filePath = Path.Combine(tempDir, fileName);
		await solution.CreateSolutionFileAsync();
		await using var helper = CreateTelemetryHelperWithExactPath(filePath, solutionPath: solution.SolutionFile);

		try
		{
			// Act - Start server which will trigger DevServer.Startup event
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			// Wait a bit for telemetry to be written
			await Task.Delay(1000, CT);
			await helper.AttemptGracefulShutdownAsync(CT);

			var fileExists = File.Exists(filePath);
			var fileContent = fileExists ? await File.ReadAllTextAsync(filePath, CT) : string.Empty;
			var events = fileContent.Length > 0 ? ParseTelemetryEvents(fileContent) : new();

			// Assert
			started.Should().BeTrue();
			fileExists.Should().BeTrue();
			events.Should().NotBeEmpty();

			// Verify that events have the EventsPrefix applied
			// The EventsPrefix should be "uno/dev-server" based on the TelemetryAttribute
			var hasEventWithPrefix = events.Any(e =>
				e.Json.RootElement.TryGetProperty("EventName", out var eventName) &&
				eventName.GetString()?.StartsWith("uno/dev-server/") == true);

			hasEventWithPrefix.Should()
				.BeTrue("Events should have the EventsPrefix 'uno/dev-server/' applied to event names");

			// Log some events for debugging
			WriteEventsList(events);
		}
		finally
		{
			await helper.StopAsync(CT);
			if (File.Exists(filePath))
			{
				try { File.Delete(filePath); }
				catch { }
			}
		}
	}
}

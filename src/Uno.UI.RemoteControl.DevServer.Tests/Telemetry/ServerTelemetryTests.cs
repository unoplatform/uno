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
		// Arrange
		var fileName = GetTestTelemetryFileName("serverconn");
		var tempDir = Path.GetTempPath();
		var filePath = Path.Combine(tempDir, fileName);
		using var solution = new SolutionHelper();
		await solution.CreateSolutionFile();
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
			await helper.AttemptGracefulShutdown(CT);
			var fileExists = File.Exists(filePath);
			var fileContent = fileExists ? await File.ReadAllTextAsync(filePath, CT) : string.Empty;
			var events = fileContent.Length > 0 ? ParseTelemetryEvents(fileContent) : new();

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
				try { File.Delete(filePath); } catch { }
			}
		}
	}
}

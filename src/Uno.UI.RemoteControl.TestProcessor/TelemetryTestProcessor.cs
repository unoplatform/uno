using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Server.Telemetry;

// Mark this assembly with the ServerProcessor attribute to make it discoverable
[assembly: ServerProcessor(typeof(Uno.UI.RemoteControl.TestProcessor.TelemetryTestProcessor))]
[assembly: Telemetry("TestProcessorKey", EventsPrefix = "uno/dev-server/test-proc")]

namespace Uno.UI.RemoteControl.TestProcessor;

/// <summary>
/// Test processor for telemetry integration testing.
/// This processor will be discovered by the RemoteControlServer during tests.
/// </summary>
public class TelemetryTestProcessor : IServerProcessor, IDisposable
{
	private readonly IRemoteControlServer _server;
	private readonly ITelemetry<TelemetryTestProcessor> _telemetry;

	/// <summary>
	/// Constructor called by the server's ActivatorUtilities.CreateInstance
	/// Will receive the ITelemetry<T> from DI if properly registered.
	/// </summary>
	public TelemetryTestProcessor(IRemoteControlServer server, ITelemetry<TelemetryTestProcessor> telemetry)
	{
		_server = server;
		_telemetry = telemetry;

		Console.WriteLine("TelemetryTestProcessor initialized");

		// Log a test event immediately to verify telemetry resolution works
		_telemetry.TrackEvent(
			"telemetry-test-initialized",
			new Dictionary<string, string> { ["ProcessorType"] = GetType().Name, ["ServerScope"] = Scope },
			new Dictionary<string, double> { ["Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
	}

	/// <summary>
	/// Scope for the processor - needs to be a valid well-known scope
	/// </summary>
	public string Scope => "TelemetryTest";

	/// <summary>
	/// Process a frame from the client
	/// </summary>
	public Task ProcessFrame(Frame frame)
	{
		Console.WriteLine("TelemetryTestProcessor processing frame...");

		_telemetry.TrackEvent(
			"telemetry-test-process-frame",
			new Dictionary<string, string> { ["FrameName"] = frame.Name },
			null);

		return Task.CompletedTask;
	}

	/// <summary>
	/// Process a message from the IDE
	/// </summary>
	public Task ProcessIdeMessage(IdeMessage message, CancellationToken ct)
	{
		_telemetry.TrackEvent(
			"telemetry-test-process-ide-message",
			new Dictionary<string, string> { ["MessageType"] = message.GetType().Name },
			null);

		return Task.CompletedTask;
	}

	public void Dispose()
	{
	}
}

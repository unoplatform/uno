using Uno.UI.RemoteControl.Server.Telemetry;

namespace Uno.HotReload.Tests.TestUtils;

/// <summary>
/// Captures the events tracked through <see cref="ITelemetry"/> so tests can assert on the dev-server's
/// hot-reload telemetry.
/// </summary>
internal sealed class RecordingTelemetry : ITelemetry
{
	private readonly List<TrackedEvent> _events = [];
	private readonly Lock _gate = new();

	public IReadOnlyList<TrackedEvent> Events
	{
		get
		{
			lock (_gate)
			{
				return [.. _events];
			}
		}
	}

	public bool Enabled => true;

	public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
	{
		lock (_gate)
		{
			_events.Add(new(
				eventName,
				new Dictionary<string, string>(properties ?? new Dictionary<string, string>()),
				new Dictionary<string, double>(measurements ?? new Dictionary<string, double>())));
		}
	}

	public void TrackEvent(string eventName, (string key, string value)[]? properties, (string key, double value)[]? measurements)
		=> TrackEvent(
			eventName,
			properties?.ToDictionary(p => p.key, p => p.value),
			measurements?.ToDictionary(m => m.key, m => m.value));

	public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
		=> TrackEvent(eventName, properties, measurements);

	public void Flush()
	{
	}

	public Task FlushAsync(CancellationToken ct)
		=> Task.CompletedTask;

	public void Dispose()
	{
	}

	internal sealed record TrackedEvent(string Name, IReadOnlyDictionary<string, string> Properties, IReadOnlyDictionary<string, double> Measurements);
}

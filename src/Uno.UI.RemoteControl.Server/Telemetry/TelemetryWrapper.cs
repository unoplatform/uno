using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.DevTools.Telemetry;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	public class TelemetryWrapper : ITelemetry
	{
		private readonly Uno.DevTools.Telemetry.Telemetry _inner;

		public TelemetryWrapper(Uno.DevTools.Telemetry.Telemetry inner)
		{
			_inner = inner;
		}

		public bool Enabled => _inner.Enabled;

		public void Dispose()
			=> _inner.Dispose();

		public void Flush()
			=> _inner.Flush();

		public Task FlushAsync(CancellationToken ct)
		{
			_inner.Flush();
			return Task.CompletedTask;
		}

		public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
			=> _inner.ThreadBlockingTrackEvent(eventName, properties, measurements);

		public void TrackEvent(string eventName, (string key, string value)[]? properties, (string key, double value)[]? measurements)
			=> _inner.TrackEvent(eventName, properties, measurements);

		public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
			=> _inner.TrackEvent(eventName, properties, measurements);
	}
}

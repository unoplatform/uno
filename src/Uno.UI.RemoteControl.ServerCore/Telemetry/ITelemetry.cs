using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	public interface ITelemetry : IDisposable
	{
		void Flush();
		Task FlushAsync(CancellationToken ct);
		void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements);
		void TrackEvent(string eventName, (string key, string value)[]? properties, (string key, double value)[]? measurements);
		void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements);
		bool Enabled { get; }
	}

	public interface ITelemetry<T> : ITelemetry;
}
